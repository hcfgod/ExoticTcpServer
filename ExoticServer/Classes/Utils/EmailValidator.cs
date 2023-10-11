using System;
using System.Text.RegularExpressions;

namespace ExoticServer.Classes.Utils
{
    public class EmailValidator
    {
        // This is a basic regex pattern for email validation. It checks for the general structure of an email but is not exhaustive.
        private static readonly string EmailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            return Regex.IsMatch(email, EmailPattern);
        }
    }
}
