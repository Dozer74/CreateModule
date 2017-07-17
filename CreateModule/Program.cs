using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CreateModule.Properties;

namespace CreateModule
{
    static class Extensions
    {
        public static string ReplacePlaceholders(this string content, Dictionary<string, string> placeholders)
        {
            var result = content;
            foreach (var pair in placeholders)
            {
                result = result.Replace(pair.Key, pair.Value);
            }
            return result;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage:\n cm <Module name>");
                return;
            }

            const string pref = "module";

            var moduleName = args[0];
            var url = moduleName.ToLower();

            var rx = new Regex(@"([a-z])([A-Z])", RegexOptions.Compiled);
            var dirName = rx.Replace(moduleName, "$1-$2").ToLower();

            if (!Directory.Exists(pref))
            {
                Console.WriteLine("Can't find \"module\" directory!");
                return;
            }

            var modulePath = $"{pref}\\{moduleName}";

            if (Directory.Exists(modulePath))
            {
                Console.WriteLine($"Module \"{moduleName}\" already exists!");
                return;
            }

            Console.Write($"Creating module \"{moduleName}\"... ");

            var placeholders = new Dictionary<string, string>
            {
                {"$moduleName$", moduleName},
                {"$moduleUrl$", url}
            };

            var configFile = Resources.configTemplate.ReplacePlaceholders(placeholders);
            var srcConfigFile = Resources.srcConfigTemplate.ReplacePlaceholders(placeholders);
            var controllerFile = Resources.controllerTemplate.ReplacePlaceholders(placeholders);
            var viewFile = Resources.viewTemplate.ReplacePlaceholders(placeholders);

            Directory.CreateDirectory(modulePath);
            Directory.CreateDirectory($"{modulePath}\\config");
            Directory.CreateDirectory($"{modulePath}\\src");
            Directory.CreateDirectory($"{modulePath}\\src\\Controller");
            Directory.CreateDirectory($"{modulePath}\\view");
            Directory.CreateDirectory($"{modulePath}\\view\\{dirName}");
            Directory.CreateDirectory($"{modulePath}\\view\\{dirName}\\{dirName}");

            File.WriteAllText($"{modulePath}\\config\\module.config.php", configFile);
            File.WriteAllText($"{modulePath}\\src\\Module.php", srcConfigFile);
            File.WriteAllText($"{modulePath}\\src\\Controller\\{moduleName}Controller.php", controllerFile);
            File.WriteAllText($"{modulePath}\\view\\{dirName}\\{dirName}\\index.phtml", viewFile);

            Console.WriteLine("Ok.");
            Console.Write("Cleaning cache... ");

            string currentDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            foreach (var cacheFile in Directory.GetFiles($"{currentDirectory}\\data\\cache", "*cache.php"))
            {
                File.Delete(cacheFile);
            }

            Console.WriteLine("Ok.");
            Console.Write("Adding to autoload... ");

            var globalConfigFile = File.ReadAllText("config\\modules.config.php");
            globalConfigFile = globalConfigFile.Insert(globalConfigFile.LastIndexOf("'") + 1,
                $",\n    \'{moduleName}\'");
            File.WriteAllText($"{currentDirectory}\\config\\modules.config.php", globalConfigFile);

            var composerFile = File.ReadAllText("composer.json");
            composerFile = composerFile.Insert(composerFile.LastIndexOf("src/\"") + 5,
                $",\n            \"{moduleName}\\\\\": \"module/{moduleName}/src/\"");

            File.WriteAllText($"{currentDirectory}\\composer.json", composerFile);

            Console.WriteLine("Ok.");
            Console.WriteLine($"Module {moduleName} successfully created!");
            Console.WriteLine("Run \"composer install\" to update autoload");
        }
    }
}