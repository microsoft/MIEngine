using Microsoft.DebugEngineHost;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MICore
{
    public static class ExceptionHelper
    {
        /// <summary>
        /// Exception filter function used to report exceptions to telemetry. This **ALWAYS** returns 'true'.
        /// </summary>
        /// <param name="currentException">The current exception which is about to be caught.</param>
        /// <param name="logger">For logging messages</param>
        /// <param name="reportOnlyCorrupting">If true, only corrupting exceptions are reported</param>
        /// <returns>true</returns>
        public static bool BeforeCatch(Exception currentException, Logger logger, bool reportOnlyCorrupting)
        {
            if (reportOnlyCorrupting && !IsCorruptingException(currentException))
            {
                return true; // ignore non-corrupting exceptions
            }

            try
            {
                HostTelemetry.ReportCurrentException(currentException, "Microsoft.MIDebugEngine");

                logger?.WriteLine("EXCEPTION: " + currentException.GetType());
                logger?.WriteTextBlock("EXCEPTION: ", currentException.StackTrace);
            }
            catch
            {
                // If anything goes wrong, ignore it. We want to report the original exception, not a telemetry problem
            }

            return true;
        }

        public static bool IsCorruptingException(Exception exception)
        {
            if (exception is NullReferenceException)
                return true;
            if (exception is ArgumentNullException)
                return true;
            if (exception is ArithmeticException)
                return true;
            if (exception is ArrayTypeMismatchException)
                return true;
            if (exception is DivideByZeroException)
                return true;
            if (exception is IndexOutOfRangeException)
                return true;
            if (exception is InvalidCastException)
                return true;
            if (exception is System.Runtime.InteropServices.SEHException)
                return true;

            return false;
        }

    }
}
