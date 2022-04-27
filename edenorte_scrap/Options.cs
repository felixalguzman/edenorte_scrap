using CommandLine;

namespace edenorte_scrap
{
    public class Options
    {
        string _email = null!;
        string _password = null!;

        string _supabaseUrl = null!;
        string _supabaseKey = null!;

        [Option('e', "email",
           Required = true,
           HelpText = "Email for Edenorte virtual")]
        public string Email
        {
            get;
            set;
        }

        [Option('p', "password",
         Required = true,
         HelpText = "Password for Edenorte virtual")]
        public string Password
        {
            get;
            set;
        }


        [Option('u', "url",
       Required = true,
       HelpText = "Supabase Url for database")]
        public string SupabaseUrl
        {
            get;
            set;
        }

        [Option('k', "key",
      Required = true,
      HelpText = "Supabase Key for database")]
        public string SupabaseKey
        {
            get;
            set;
        }

        static void ParseAndAssign(string? value, Action<string> assign)
        {
            if (value is { Length: > 0 } && assign is not null)
            {
                assign(value.Split("/")[^1]);
            }
        }
    }
}
