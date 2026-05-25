using System.IO.Compression;
using System.Net;

namespace CasUnMultiplayerAutoInstaller
{
    // Source - https://stackoverflow.com/a/66270371
    // Posted by Tony
    // Retrieved 2026-05-25, License - CC BY-SA 4.0

    public static class HttpClientUtils
    {
        public static async Task DownloadFileTaskAsync(this HttpClient client, Uri uri, string FileName)
        {
            using (var s = await client.GetStreamAsync(uri))
            {
                using (var fs = new FileStream(FileName, FileMode.CreateNew))
                {
                    await s.CopyToAsync(fs);
                }
            }
        }
    }

    internal class Program
    {
        static void CopyDirectory(DirectoryInfo source, DirectoryInfo destination)
        {
            if (!destination.Exists)
            {
                destination.Create();
            }

            // Copy all files.
            FileInfo[] files = source.GetFiles();
            foreach (FileInfo file in files)
            {
                file.CopyTo(Path.Combine(destination.FullName,
                    file.Name), true);
            }

            // Process subdirectories.
            DirectoryInfo[] dirs = source.GetDirectories();
            foreach (DirectoryInfo dir in dirs)
            {
                // Get destination directory.
                string destinationDir = Path.Combine(destination.FullName, dir.Name);

                // Call CopyDirectory() recursively.
                CopyDirectory(dir, new DirectoryInfo(destinationDir));
            }
        }
        // Source - https://stackoverflow.com/a/329502
        // Posted by Jeremy Edwards, modified by community. See post 'Timeline' for change history
        // Retrieved 2026-05-25, License - CC BY-SA 3.0

        public static void DeleteDirectory(string target_dir)
        {
            string[] files = Directory.GetFiles(target_dir);
            string[] dirs = Directory.GetDirectories(target_dir);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(target_dir, false);
        }

        static string ModURL = "https://github.com/Krokosha666/cas-unk-krokosha-multiplayer-coop/releases/download/v3.0.0/KrokMP_3.0.0_release_game6.1demo_bepinex5.4.23.5.zip";
        static string BepInExURL = "https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.5/BepInEx_win_x64_5.4.23.5.zip";
        static string TempPATH = "./tempfiles_casunknown";
        static string[] defaultSteamPathArray = { "\\Program Files (x86)\\Steam\\steamapps\\common\\Casualties Unknown Demo", "\\Program Files\\Steam\\steamapps\\common\\Casualties Unknown Demo" };
        static string? FindSteamGameFolderLocation()
        {
            DriveInfo[] drives = DriveInfo.GetDrives();
            for (int i = 0; i < drives.Length; i++)
            {
                for (int j = 0; j < defaultSteamPathArray.Length; j++)
                {
                    string path = $"{drives[i].Name.Remove(drives[i].Name.Length - 1)}{defaultSteamPathArray[j]}";
                    Console.WriteLine("Checking " + path);
                    if (Directory.Exists(path))
                    {
                        Console.WriteLine("Found Steam Library Folder");

                        return path;
                    }
                }
            }

            return null;
        }
        static async Task DownloadFiles()
        {
            HttpClient client = new();

            if (Directory.Exists(TempPATH)) { Directory.Delete(TempPATH, true); }
            if (!Directory.Exists(TempPATH)) { Directory.CreateDirectory(TempPATH); }

            await HttpClientUtils.DownloadFileTaskAsync(client, new Uri(ModURL), $"{TempPATH}\\mod.zip");
            Console.WriteLine("Done!");
            Console.WriteLine("Downloading BepInEx File...");
            await HttpClientUtils.DownloadFileTaskAsync(client, new Uri(BepInExURL), $"{TempPATH}\\bepinex.zip");
            Console.WriteLine("Done!");

            ZipArchive zip_mod = ZipFile.OpenRead($"{TempPATH}\\mod.zip");
            ZipArchive zip_bepinex = ZipFile.OpenRead($"{TempPATH}\\bepinex.zip");

            Console.WriteLine("Unzipping Mod File...");
            if (!Directory.Exists($"{TempPATH}\\mod")) { Directory.CreateDirectory($"{TempPATH}\\mod"); }
            zip_mod.ExtractToDirectory($"{TempPATH}\\mod");
            Console.WriteLine("Done!");

            Console.WriteLine("Unzipping BepInEx File...");
            if (!Directory.Exists($"{TempPATH}\\bepinex")) { Directory.CreateDirectory($"{TempPATH}\\bepinex"); }
            zip_bepinex.ExtractToDirectory($"{TempPATH}\\bepinex");
            Console.WriteLine("Done!");

            zip_bepinex.Dispose();
            zip_mod.Dispose();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Task 1 Completed Successfully.");
            Console.ForegroundColor = ConsoleColor.White;
        }
        static void MoveFiles(string steam_path)
        {
            Console.WriteLine("Moving files...");

            DirectoryInfo steam_path_info = new DirectoryInfo(steam_path);
            CopyDirectory(new DirectoryInfo($"{TempPATH}\\bepinex"), steam_path_info);
            Console.WriteLine("Move 1");
            CopyDirectory(new DirectoryInfo($"{TempPATH}\\mod\\mod"), steam_path_info);
            Console.WriteLine("Move 2");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Task 2 Completed Successfully.");
            Console.ForegroundColor = ConsoleColor.White;
        }
        static void Cleanup()
        {
            Console.WriteLine("Cleaning up Temp folder...");
            DeleteDirectory(TempPATH);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Task 3 Completed Successfully.");
            Console.ForegroundColor = ConsoleColor.White;
        }
        static async Task Start()
        {
            string? location = FindSteamGameFolderLocation();

            if (location == null)
            {
                Console.WriteLine("Couldn't find game folder location. Please go to Steam -> Library -> Casualities Unknown -> Gear Icon -> Properties -> Local Files -> Browse; And then copy paste the location here.");
                Console.WriteLine("\nGame Folder Path: ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                location = Console.ReadLine();
                Console.ForegroundColor = ConsoleColor.White;

                if (!Directory.Exists(location))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("The path inputted does not exist. Closing program.");

                    Console.ReadKey(true);

                    Environment.Exit(0);
                }
            }

            Console.ForegroundColor = ConsoleColor.White;
            try
            {
                await DownloadFiles();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error occured while downloading files: " + ex.Message);
                Console.ReadKey(true);
                Environment.Exit(-1);
            }

            try
            {
                MoveFiles(location);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error occured while moving files: " + ex.Message);
                Console.ReadKey(true);
                Environment.Exit(-1);
            }

            try
            {
                Cleanup();
            }
            catch (Exception) { }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\nAll tasks completed successfully! Make sure to verify the integrity of game files if any issues occur. Have Fun ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("Dying");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("!\n");
            Console.ForegroundColor = ConsoleColor.White;

            Console.ReadKey(true);
        }
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("This executable is for V6.1dG and V3.0M!");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Gathering Resources... Please make sure you have a stable internet connection.");

            Start().Wait();
        }
    }
}
