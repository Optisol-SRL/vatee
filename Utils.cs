﻿using System.Collections.Generic;
using System.Text;

namespace Vatee;

public static class Utils
{
    public static readonly Dictionary<char, char> DiacriticsMap =
        new()
        {
            ['ț'] = 't',
            ['Ț'] = 'T',
            ['ș'] = 's',
            ['Ș'] = 'S',
            ['ş'] = 's',
            ['Ş'] = 'S',
            ['ţ'] = 't',
            ['Ţ'] = 'T',
            ['ă'] = 'a',
            ['Ă'] = 'A',
            ['â'] = 'a',
            ['Â'] = 'A',
            ['î'] = 'i',
            ['Î'] = 'I',
        };

    public static string FlattenRomanianDiacritics(string str)
    {
        var sb = new StringBuilder(str.Length);
        for (var i = 0; i < str.Length; i++)
        {
            var chr = str[i];
            sb.Append(DiacriticsMap.GetValueOrDefault(chr, chr));
        }

        return sb.ToString();
    }
}
