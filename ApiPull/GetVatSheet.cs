using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Flurl.Http.Configuration;
using Newtonsoft.Json;

namespace Vatee.ApiPull;

public class GetVatSheet
{
    public async Task<Response> Get(int fiscalId, DateOnly date, X509Certificate2 clientCert, CancellationToken token = default)
    {
        // ideally we'd store 1 of these per thumbprint for the lifetime of the app
        var client = new FlurlClientBuilder()
            .ConfigureInnerHandler(h =>
            {
                h.ClientCertificates.Add(clientCert);
            })
            .Build();

        var url = new Url("https://webserviceapl.anaf.ro/decont/ws/v1/info").SetQueryParams(
            new
            {
                cui = fiscalId,
                an = date.Year,
                luna = date.Month,
            }
        );
        var response = await client.Request(url).GetAsync(HttpCompletionOption.ResponseContentRead, token);

        if (response.ResponseMessage?.RequestMessage?.RequestUri?.Host == "mentenanta.anaf.ro")
        {
            return new Response { StatusCode = response.StatusCode, ResultType = VatSheetCheckResult.Maintenance };
        }

        var contentType = GetContentType(response);
        if (response.StatusCode == 200 && contentType == KnownMimeTypes.Zip)
        {
            var file = await ReadFile(response);
            return new Response
            {
                ResultType = VatSheetCheckResult.Success,
                StatusCode = response.StatusCode,
                SuccessMessage = new SuccessApiType { Archive = file },
            };
        }

        if (response.StatusCode == 200 && contentType == KnownMimeTypes.Json)
        {
            var responseText = await response.GetStringAsync();
            return new Response
            {
                BodyString = responseText,
                StatusCode = response.StatusCode,
                ErrorMessage = JsonConvert.DeserializeObject<ErrorApiType>(responseText),
                ResultType = VatSheetCheckResult.ErrorWithMessage,
            };
        }

        throw new Exception("Could not read response");
    }

    private static async Task<DownloadedFile> ReadFile(IFlurlResponse response)
    {
        var bytes = await response.GetBytesAsync();
        var headers = response.ResponseMessage.Content.Headers;
        return new DownloadedFile
        {
            Content = bytes,
            MimeType = headers.ContentType?.MediaType,
            FileName = headers.ContentDisposition?.FileNameStar ?? headers.ContentDisposition?.FileName,
        };
    }

    private static string GetContentType(IFlurlResponse response)
    {
        var contentType = response.ResponseMessage.Content.Headers.ContentType;
        return contentType?.MediaType ?? string.Empty;
    }

    public class Response
    {
        public VatSheetCheckResult ResultType { get; set; }
        public SuccessApiType SuccessMessage { get; set; }
        public ErrorApiType ErrorMessage { get; set; }
        public int StatusCode { get; set; }
        public string BodyString { get; set; }
    }

    public class SuccessApiType
    {
        public DownloadedFile Archive { get; set; }
    }

    public class ErrorApiType
    {
        [JsonProperty("trace_id")]
        public string TraceId { get; set; }

        [JsonProperty("date_response")]
        public string DateResponse { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }

        public VatSheetCheckError GetErrorType()
        {
            if (string.IsNullOrWhiteSpace(Error))
            {
                return VatSheetCheckError.Unknown;
            }

            if (Error.Contains("CUI=") && Error.Contains("este invalid"))
            {
                return VatSheetCheckError.InvalidFiscal;
            }

            if (Error.Contains("An=") && Error.Contains("este invalid"))
            {
                return VatSheetCheckError.InvalidYear;
            }

            if (Error.Contains("An=") && Error.Contains("este mai mic decat 2024"))
            {
                return VatSheetCheckError.YearBefore2024;
            }

            if (Error.Contains("Luna=") && Error.Contains("este invalida"))
            {
                return VatSheetCheckError.InvalidMonth;
            }

            if (Error.Contains("Pentru anul 2024, luna trebuie sa fie >= 7"))
            {
                return VatSheetCheckError.DateBefore202407;
            }

            if (Error.Contains("Nu exista raspuns pentru"))
            {
                return VatSheetCheckError.SuccessNotFound;
            }

            if (Error.Contains("S-au efectuat deja") && Error.Contains("apeluri pentru cui"))
            {
                return VatSheetCheckError.TooManyRequests;
            }

            if (Error.Contains("Nu aveti drept in SPV pentru CIF"))
            {
                return VatSheetCheckError.NoAccessToFiscalId;
            }

            if (Error.Contains("Nu exista niciun CIF pentru care sa aveti drept in SPV"))
            {
                return VatSheetCheckError.NoAccessToInvoices;
            }

            if (Error.Contains("A aparut o eroare tehnica"))
            {
                return VatSheetCheckError.Generic;
            }

            return VatSheetCheckError.Unknown;
        }
    }
}

public enum VatSheetCheckResult
{
    Success,
    ErrorWithMessage,
    Maintenance,
}

public enum VatSheetCheckError
{
    Unknown = 0,
    SuccessNotFound = 103,
    InvalidFiscal = 201,
    InvalidYear = 202,
    YearBefore2024 = 203,
    InvalidMonth = 204,
    DateBefore202407 = 205,
    NoAccessToInvoices = 206,
    NoAccessToFiscalId = 207,
    Generic = 208,
    TooManyRequests = 209,
}

public class DownloadedFile
{
    public byte[] Content { get; set; }
    public string FileName { get; set; }
    public string MimeType { get; set; }
}
