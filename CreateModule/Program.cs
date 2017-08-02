using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using CreateModule.Properties;

namespace CreateModule
{
    static class Extensions
    {
        public static string Render(this string content, Dictionary<string, string> placeholders)
        {
            var result = content;
            foreach (var pair in placeholders)
            {
                result = result.Replace($"{{{{{pair.Key}}}}}", pair.Value);
            }
            return result;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            #region Arguments
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: create-zend-module <name> [-p | --path <path>]");
                Console.WriteLine("\t-p | --path <path>\tPath to Zend project root directory");
                return;
            }

            string moduleName = args[0];
            string path = Environment.CurrentDirectory;

            if (args.Length == 2 || args.Length == 3)
            {
                switch (args[1])
                {
                    case "-p":
                    case "--path":
                        if (args.Length == 3)
                        {
                            path = Path.GetFullPath(Path.Combine(path, args[2]));
                        }
                        break;
                    default:
                        Console.WriteLine($"Unknown option: {args[1]}");
                        return;
                }
            }
            #endregion

            string moduleRoot = $"{path}\\module";
            string moduleDirectory = $"{path}\\module\\{moduleName}";
            string moduleUrl = moduleName.ToLower();
            string moduleViewName  = Regex.Replace(moduleName, @"([a-z])([A-Z])", "$1-$2").ToLower();

            if (!Directory.Exists(moduleRoot))
            {
                Console.WriteLine("Can't find \"module\" directory.");
                return;
            }

            if (Directory.Exists(moduleDirectory))
            {
                Console.WriteLine($"Module \"{moduleName}\" already exists.");
                return;
            }

            #region Templates
            Console.Write($"Creating module \"{moduleName}\"... ");

            var data = new Dictionary<string, string>
            {
                {"moduleName", moduleName},
                {"moduleUrl" , moduleUrl }
            };

            var configText       = Resources.ConfigTemplate.Render(data);
            var sourceConfigText = Resources.SourceConfigTemplate.Render(data);
            var controllerText   = Resources.ControllerTemplate.Render(data);
            var viewText         = Resources.ViewTemplate.Render(data);

            Directory.CreateDirectory(moduleDirectory);
            Directory.CreateDirectory($"{moduleDirectory}\\config");
            Directory.CreateDirectory($"{moduleDirectory}\\src");
            Directory.CreateDirectory($"{moduleDirectory}\\src\\Controller");
            Directory.CreateDirectory($"{moduleDirectory}\\view");
            Directory.CreateDirectory($"{moduleDirectory}\\view\\{moduleViewName}");
            Directory.CreateDirectory($"{moduleDirectory}\\view\\{moduleViewName}\\{moduleViewName}");

            File.WriteAllText($"{moduleDirectory}\\config\\module.config.php", configText);
            File.WriteAllText($"{moduleDirectory}\\src\\Module.php", sourceConfigText);
            File.WriteAllText($"{moduleDirectory}\\src\\Controller\\{moduleName}Controller.php", controllerText);
            File.WriteAllText($"{moduleDirectory}\\view\\{moduleViewName}\\{moduleViewName}\\index.phtml", viewText);

            Console.WriteLine("OK.");
            #endregion

            #region Chache
            Console.Write("Cleaning cache... ");

            string cacheDirectory = $"{path}\\data\\cache";
            if (Directory.Exists(cacheDirectory))
            {
                foreach (var cacheFile in Directory.GetFiles(cacheDirectory, "*cache.php"))
                {
                    File.Delete(cacheFile);
                }
            }

            Console.WriteLine("OK.");
            #endregion

            #region Autoload
            Console.Write("Adding to autoload... ");

            try
            {
                // WTF magic?!
                var globalConfigText = File.ReadAllText($"{path}\\config\\modules.config.php");
                globalConfigText = globalConfigText.Insert(
                    globalConfigText.LastIndexOf("'") + 1,
                    $",\n    \'{moduleName}\'");

                File.WriteAllText($"{path}\\config\\modules.config.php", globalConfigText);

                // WTF magic?!
                var composerFile = File.ReadAllText($"{path}\\composer.json");
                composerFile = composerFile.Insert(
                    composerFile.LastIndexOf("src/\"") + 5,
                    $",\n            \"{moduleName}\\\\\": \"module/{moduleName}/src/\"");

                File.WriteAllText($"{path}\\composer.json", composerFile);

            }
            catch {}

            Console.WriteLine("OK.");
            #endregion

            Console.WriteLine($"Module {moduleName} successfully created. Run \"composer install\" to update autoload.");
        }
    }
}
