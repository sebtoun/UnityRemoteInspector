﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RemoteInspector.Server
{
    public static class ServerUtility
    {
        private static Dictionary<string, char> _entities;
        private static object _sync = new object();

        /// <summary>
        /// Decodes an HTML-encoded <see cref="string"/> and returns the decoded <see cref="string"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the decoded string.
        /// </returns>
        /// <param name="s">
        /// A <see cref="string"/> to decode.
        /// </param>
        public static string HtmlDecode( string s )
        {
            if ( string.IsNullOrEmpty( s ) || !s.Contains( "&" ) )
                return s;
            var entity = new StringBuilder();
            var output = new StringBuilder();

            // 0 -> nothing,
            // 1 -> right after '&'
            // 2 -> between '&' and ';' but no '#'
            // 3 -> '#' found after '&' and getting numbers
            var state = 0;
            var number = 0;
            var haveTrailingDigits = false;
            foreach ( var c in s )
            {
                if ( state == 0 )
                {
                    if ( c == '&' )
                    {
                        entity.Append( c );
                        state = 1;
                    }
                    else
                    {
                        output.Append( c );
                    }

                    continue;
                }

                if ( c == '&' )

                {
                    state = 1;
                    if ( haveTrailingDigits )
                    {
                        entity.Append( number.ToString( CultureInfo.InvariantCulture ) );
                        haveTrailingDigits = false;
                    }

                    output.Append( entity.ToString() );
                    entity.Length = 0;
                    entity.Append( '&' );
                    continue;
                }

                if ( state == 1 )
                {
                    if ( c == ';' )
                    {
                        state = 0;
                        output.Append( entity.ToString() );
                        output.Append( c );
                        entity.Length = 0;
                    }
                    else
                    {
                        number = 0;
                        if ( c != '#' )
                            state = 2;
                        else

                            state = 3;
                        entity.Append( c );
                    }
                }
                else if ( state == 2 )
                {
                    entity.Append( c );
                    if ( c == ';' )
                    {
                        var key = entity.ToString();
                        var entities = getEntities();
                        if ( key.Length > 1 && entities.ContainsKey( key.Substring( 1, key.Length - 2 ) ) )
                            key = entities[ key.Substring( 1, key.Length - 2 ) ].ToString();
                        output.Append( key );
                        state = 0;
                        entity.Length = 0;
                    }
                }
                else if ( state == 3 )
                {
                    if ( c == ';' )
                    {
                        if ( number > 65535 )
                        {
                            output.Append( "&#" );
                            output.Append( number.ToString( CultureInfo.InvariantCulture ) );
                            output.Append( ";" );
                        }
                        else
                        {
                            output.Append( (char) number );
                        }

                        state = 0;
                        entity.Length = 0;
                        haveTrailingDigits = false;
                    }
                    else if ( Char.IsDigit( c ) )
                    {
                        number = number * 10 + ( (int) c - '0' );
                        haveTrailingDigits = true;
                    }
                    else
                    {
                        state = 2;
                        if ( haveTrailingDigits )
                        {
                            entity.Append( number.ToString( CultureInfo.InvariantCulture ) );
                            haveTrailingDigits = false;
                        }

                        entity.Append( c );
                    }
                }
            }

            if ( entity.Length > 0 )
                output.Append( entity.ToString() );
            else if ( haveTrailingDigits )
                output.Append( number.ToString( CultureInfo.InvariantCulture ) );
            return output.ToString();
        }

        public static string UrlDecode( string s )
        {
            return UrlDecode( s, Encoding.UTF8 );
        }

        public static string UrlDecode( string s, Encoding encoding )
        {
            if ( string.IsNullOrEmpty( s ) || !( s.Contains( "%" ) || s.Contains( "+" ) ) )
                return s;

            if ( encoding == null )
                encoding = Encoding.UTF8;

            var buff = new List<byte>();
            var len = s.Length;
            for ( var i = 0; i < len; i++ )
            {
                var c = s[ i ];
                if ( c == '%' && i + 2 < len && s[ i + 1 ] != '%' )
                {
                    int xchar;
                    if ( s[ i + 1 ] == 'u' && i + 5 < len )
                    {
                        // Unicode hex sequence.
                        xchar = getChar( s, i + 2, 4 );
                        if ( xchar != -1 )
                        {
                            writeCharBytes( (char) xchar, buff, encoding );
                            i += 5;
                        }
                        else
                        {
                            writeCharBytes( '%', buff, encoding );
                        }
                    }
                    else if ( ( xchar = getChar( s, i + 1, 2 ) ) != -1 )
                    {
                        writeCharBytes( (char) xchar, buff, encoding );
                        i += 2;
                    }
                    else
                    {
                        writeCharBytes( '%', buff, encoding );
                    }

                    continue;
                }

                if ( c == '+' )
                {
                    writeCharBytes( ' ', buff, encoding );
                    continue;
                }

                writeCharBytes( c, buff, encoding );
            }

            return encoding.GetString( buff.ToArray() );
        }

        private static int getChar( string s, int offset, int length )
        {
            var val = 0;
            var end = length + offset;
            for ( var i = offset; i < end; i++ )
            {
                var c = s[ i ];
                if ( c > 127 )
                    return -1;

                var current = getInt( (byte) c );
                if ( current == -1 )
                    return -1;

                val = ( val << 4 ) + current;
            }

            return val;
        }

        private static int getInt( byte b )
        {
            var c = (char) b;
            return c >= '0' && c <= '9'
                ? c - '0'
                : c >= 'a' && c <= 'f'
                    ? c - 'a' + 10
                    : c >= 'A' && c <= 'F'
                        ? c - 'A' + 10
                        : -1;
        }

        private static void writeCharBytes( char c, IList buffer, Encoding encoding )
        {
            if ( c > 255 )
            {
                foreach ( var b in encoding.GetBytes( new[] { c } ) )
                    buffer.Add( b );

                return;
            }

            buffer.Add( (byte) c );
        }

        private static Dictionary<string, char> getEntities()
        {
            lock ( _sync )
            {
                if ( _entities == null )
                    initEntities();
                return _entities;
            }
        }

        private static void initEntities()
        {
            // Build the dictionary of HTML entity references.
            // This list comes from the HTML 4.01 W3C recommendation.
            _entities = new Dictionary<string, char>();
            _entities.Add( "nbsp", '\u00A0' );
            _entities.Add( "iexcl", '\u00A1' );
            _entities.Add( "cent", '\u00A2' );
            _entities.Add( "pound", '\u00A3' );
            _entities.Add( "curren", '\u00A4' );
            _entities.Add( "yen", '\u00A5' );
            _entities.Add( "brvbar", '\u00A6' );
            _entities.Add( "sect", '\u00A7' );
            _entities.Add( "uml", '\u00A8' );
            _entities.Add( "copy", '\u00A9' );
            _entities.Add( "ordf", '\u00AA' );
            _entities.Add( "laquo", '\u00AB' );
            _entities.Add( "not", '\u00AC' );
            _entities.Add( "shy", '\u00AD' );
            _entities.Add( "reg", '\u00AE' );
            _entities.Add( "macr", '\u00AF' );
            _entities.Add( "deg", '\u00B0' );
            _entities.Add( "plusmn", '\u00B1' );
            _entities.Add( "sup2", '\u00B2' );
            _entities.Add( "sup3", '\u00B3' );
            _entities.Add( "acute", '\u00B4' );
            _entities.Add( "micro", '\u00B5' );
            _entities.Add( "para", '\u00B6' );
            _entities.Add( "middot", '\u00B7' );
            _entities.Add( "cedil", '\u00B8' );
            _entities.Add( "sup1", '\u00B9' );
            _entities.Add( "ordm", '\u00BA' );
            _entities.Add( "raquo", '\u00BB' );
            _entities.Add( "frac14", '\u00BC' );
            _entities.Add( "frac12", '\u00BD' );
            _entities.Add( "frac34", '\u00BE' );
            _entities.Add( "iquest", '\u00BF' );
            _entities.Add( "Agrave", '\u00C0' );
            _entities.Add( "Aacute", '\u00C1' );
            _entities.Add( "Acirc", '\u00C2' );
            _entities.Add( "Atilde", '\u00C3' );
            _entities.Add( "Auml", '\u00C4' );
            _entities.Add( "Aring", '\u00C5' );
            _entities.Add( "AElig", '\u00C6' );
            _entities.Add( "Ccedil", '\u00C7' );
            _entities.Add( "Egrave", '\u00C8' );
            _entities.Add( "Eacute", '\u00C9' );
            _entities.Add( "Ecirc", '\u00CA' );
            _entities.Add( "Euml", '\u00CB' );
            _entities.Add( "Igrave", '\u00CC' );
            _entities.Add( "Iacute", '\u00CD' );
            _entities.Add( "Icirc", '\u00CE' );
            _entities.Add( "Iuml", '\u00CF' );
            _entities.Add( "ETH", '\u00D0' );
            _entities.Add( "Ntilde", '\u00D1' );
            _entities.Add( "Ograve", '\u00D2' );
            _entities.Add( "Oacute", '\u00D3' );
            _entities.Add( "Ocirc", '\u00D4' );
            _entities.Add( "Otilde", '\u00D5' );
            _entities.Add( "Ouml", '\u00D6' );
            _entities.Add( "times", '\u00D7' );
            _entities.Add( "Oslash", '\u00D8' );
            _entities.Add( "Ugrave", '\u00D9' );
            _entities.Add( "Uacute", '\u00DA' );
            _entities.Add( "Ucirc", '\u00DB' );
            _entities.Add( "Uuml", '\u00DC' );
            _entities.Add( "Yacute", '\u00DD' );
            _entities.Add( "THORN", '\u00DE' );
            _entities.Add( "szlig", '\u00DF' );
            _entities.Add( "agrave", '\u00E0' );
            _entities.Add( "aacute", '\u00E1' );
            _entities.Add( "acirc", '\u00E2' );
            _entities.Add( "atilde", '\u00E3' );
            _entities.Add( "auml", '\u00E4' );
            _entities.Add( "aring", '\u00E5' );
            _entities.Add( "aelig", '\u00E6' );
            _entities.Add( "ccedil", '\u00E7' );
            _entities.Add( "egrave", '\u00E8' );
            _entities.Add( "eacute", '\u00E9' );
            _entities.Add( "ecirc", '\u00EA' );
            _entities.Add( "euml", '\u00EB' );
            _entities.Add( "igrave", '\u00EC' );
            _entities.Add( "iacute", '\u00ED' );
            _entities.Add( "icirc", '\u00EE' );
            _entities.Add( "iuml", '\u00EF' );
            _entities.Add( "eth", '\u00F0' );
            _entities.Add( "ntilde", '\u00F1' );
            _entities.Add( "ograve", '\u00F2' );
            _entities.Add( "oacute", '\u00F3' );
            _entities.Add( "ocirc", '\u00F4' );
            _entities.Add( "otilde", '\u00F5' );
            _entities.Add( "ouml", '\u00F6' );
            _entities.Add( "divide", '\u00F7' );
            _entities.Add( "oslash", '\u00F8' );
            _entities.Add( "ugrave", '\u00F9' );
            _entities.Add( "uacute", '\u00FA' );
            _entities.Add( "ucirc", '\u00FB' );
            _entities.Add( "uuml", '\u00FC' );
            _entities.Add( "yacute", '\u00FD' );
            _entities.Add( "thorn", '\u00FE' );
            _entities.Add( "yuml", '\u00FF' );
            _entities.Add( "fnof", '\u0192' );
            _entities.Add( "Alpha", '\u0391' );
            _entities.Add( "Beta", '\u0392' );
            _entities.Add( "Gamma", '\u0393' );
            _entities.Add( "Delta", '\u0394' );
            _entities.Add( "Epsilon", '\u0395' );
            _entities.Add( "Zeta", '\u0396' );
            _entities.Add( "Eta", '\u0397' );
            _entities.Add( "Theta", '\u0398' );
            _entities.Add( "Iota", '\u0399' );
            _entities.Add( "Kappa", '\u039A' );
            _entities.Add( "Lambda", '\u039B' );
            _entities.Add( "Mu", '\u039C' );
            _entities.Add( "Nu", '\u039D' );
            _entities.Add( "Xi", '\u039E' );
            _entities.Add( "Omicron", '\u039F' );
            _entities.Add( "Pi", '\u03A0' );
            _entities.Add( "Rho", '\u03A1' );
            _entities.Add( "Sigma", '\u03A3' );
            _entities.Add( "Tau", '\u03A4' );
            _entities.Add( "Upsilon", '\u03A5' );
            _entities.Add( "Phi", '\u03A6' );
            _entities.Add( "Chi", '\u03A7' );
            _entities.Add( "Psi", '\u03A8' );
            _entities.Add( "Omega", '\u03A9' );
            _entities.Add( "alpha", '\u03B1' );
            _entities.Add( "beta", '\u03B2' );
            _entities.Add( "gamma", '\u03B3' );
            _entities.Add( "delta", '\u03B4' );
            _entities.Add( "epsilon", '\u03B5' );
            _entities.Add( "zeta", '\u03B6' );
            _entities.Add( "eta", '\u03B7' );
            _entities.Add( "theta", '\u03B8' );
            _entities.Add( "iota", '\u03B9' );
            _entities.Add( "kappa", '\u03BA' );
            _entities.Add( "lambda", '\u03BB' );
            _entities.Add( "mu", '\u03BC' );
            _entities.Add( "nu", '\u03BD' );
            _entities.Add( "xi", '\u03BE' );
            _entities.Add( "omicron", '\u03BF' );
            _entities.Add( "pi", '\u03C0' );
            _entities.Add( "rho", '\u03C1' );
            _entities.Add( "sigmaf", '\u03C2' );
            _entities.Add( "sigma", '\u03C3' );
            _entities.Add( "tau", '\u03C4' );
            _entities.Add( "upsilon", '\u03C5' );
            _entities.Add( "phi", '\u03C6' );
            _entities.Add( "chi", '\u03C7' );
            _entities.Add( "psi", '\u03C8' );
            _entities.Add( "omega", '\u03C9' );
            _entities.Add( "thetasym", '\u03D1' );
            _entities.Add( "upsih", '\u03D2' );
            _entities.Add( "piv", '\u03D6' );
            _entities.Add( "bull", '\u2022' );
            _entities.Add( "hellip", '\u2026' );
            _entities.Add( "prime", '\u2032' );
            _entities.Add( "Prime", '\u2033' );
            _entities.Add( "oline", '\u203E' );
            _entities.Add( "frasl", '\u2044' );
            _entities.Add( "weierp", '\u2118' );
            _entities.Add( "image", '\u2111' );
            _entities.Add( "real", '\u211C' );
            _entities.Add( "trade", '\u2122' );
            _entities.Add( "alefsym", '\u2135' );
            _entities.Add( "larr", '\u2190' );
            _entities.Add( "uarr", '\u2191' );
            _entities.Add( "rarr", '\u2192' );
            _entities.Add( "darr", '\u2193' );
            _entities.Add( "harr", '\u2194' );
            _entities.Add( "crarr", '\u21B5' );
            _entities.Add( "lArr", '\u21D0' );
            _entities.Add( "uArr", '\u21D1' );
            _entities.Add( "rArr", '\u21D2' );
            _entities.Add( "dArr", '\u21D3' );
            _entities.Add( "hArr", '\u21D4' );
            _entities.Add( "forall", '\u2200' );
            _entities.Add( "part", '\u2202' );
            _entities.Add( "exist", '\u2203' );
            _entities.Add( "empty", '\u2205' );
            _entities.Add( "nabla", '\u2207' );
            _entities.Add( "isin", '\u2208' );
            _entities.Add( "notin", '\u2209' );
            _entities.Add( "ni", '\u220B' );
            _entities.Add( "prod", '\u220F' );
            _entities.Add( "sum", '\u2211' );
            _entities.Add( "minus", '\u2212' );
            _entities.Add( "lowast", '\u2217' );
            _entities.Add( "radic", '\u221A' );
            _entities.Add( "prop", '\u221D' );
            _entities.Add( "infin", '\u221E' );
            _entities.Add( "ang", '\u2220' );
            _entities.Add( "and", '\u2227' );
            _entities.Add( "or", '\u2228' );
            _entities.Add( "cap", '\u2229' );
            _entities.Add( "cup", '\u222A' );
            _entities.Add( "int", '\u222B' );
            _entities.Add( "there4", '\u2234' );
            _entities.Add( "sim", '\u223C' );
            _entities.Add( "cong", '\u2245' );
            _entities.Add( "asymp", '\u2248' );
            _entities.Add( "ne", '\u2260' );
            _entities.Add( "equiv", '\u2261' );
            _entities.Add( "le", '\u2264' );
            _entities.Add( "ge", '\u2265' );
            _entities.Add( "sub", '\u2282' );
            _entities.Add( "sup", '\u2283' );
            _entities.Add( "nsub", '\u2284' );
            _entities.Add( "sube", '\u2286' );
            _entities.Add( "supe", '\u2287' );
            _entities.Add( "oplus", '\u2295' );
            _entities.Add( "otimes", '\u2297' );
            _entities.Add( "perp", '\u22A5' );
            _entities.Add( "sdot", '\u22C5' );
            _entities.Add( "lceil", '\u2308' );
            _entities.Add( "rceil", '\u2309' );
            _entities.Add( "lfloor", '\u230A' );
            _entities.Add( "rfloor", '\u230B' );
            _entities.Add( "lang", '\u2329' );
            _entities.Add( "rang", '\u232A' );
            _entities.Add( "loz", '\u25CA' );
            _entities.Add( "spades", '\u2660' );
            _entities.Add( "clubs", '\u2663' );
            _entities.Add( "hearts", '\u2665' );
            _entities.Add( "diams", '\u2666' );
            _entities.Add( "quot", '\u0022' );
            _entities.Add( "amp", '\u0026' );
            _entities.Add( "lt", '\u003C' );
            _entities.Add( "gt", '\u003E' );
            _entities.Add( "OElig", '\u0152' );
            _entities.Add( "oelig", '\u0153' );
            _entities.Add( "Scaron", '\u0160' );
            _entities.Add( "scaron", '\u0161' );
            _entities.Add( "Yuml", '\u0178' );
            _entities.Add( "circ", '\u02C6' );
            _entities.Add( "tilde", '\u02DC' );
            _entities.Add( "ensp", '\u2002' );
            _entities.Add( "emsp", '\u2003' );
            _entities.Add( "thinsp", '\u2009' );
            _entities.Add( "zwnj", '\u200C' );
            _entities.Add( "zwj", '\u200D' );
            _entities.Add( "lrm", '\u200E' );
            _entities.Add( "rlm", '\u200F' );
            _entities.Add( "ndash", '\u2013' );
            _entities.Add( "mdash", '\u2014' );
            _entities.Add( "lsquo", '\u2018' );
            _entities.Add( "rsquo", '\u2019' );
            _entities.Add( "sbquo", '\u201A' );
            _entities.Add( "ldquo", '\u201C' );
            _entities.Add( "rdquo", '\u201D' );
            _entities.Add( "bdquo", '\u201E' );
            _entities.Add( "dagger", '\u2020' );
            _entities.Add( "Dagger", '\u2021' );
            _entities.Add( "permil", '\u2030' );
            _entities.Add( "lsaquo", '\u2039' );
            _entities.Add( "rsaquo", '\u203A' );
            _entities.Add( "euro", '\u20AC' );
        }
    }
}