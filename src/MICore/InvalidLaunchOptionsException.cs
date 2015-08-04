using System;
using System.Globalization;

namespace MICore
{
    public class InvalidLaunchOptionsException : Exception
    {
        internal InvalidLaunchOptionsException(string problemDescription) : 
            base(string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_InvalidLaunchOptions, problemDescription))
        {
        }
    }
}