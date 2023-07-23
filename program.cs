using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Carpark
{
    class Program
    {
        static string pathToUsers = AppDomain.CurrentDomain.BaseDirectory + "/Users.txt";
        static string pathToVehicles = AppDomain.CurrentDomain.BaseDirectory + "/Vehicles.txt";

        static User loggedInUser;


        static void Main(string[] args)
        {
            Console.WriteLine("Program pro správu rezervací a vozového parku.");
            Console.WriteLine();
            Console.WriteLine("Cesta k souborům: " + AppDomain.CurrentDomain.BaseDirectory);

            if (!File.Exists(pathToUsers))
            {
                new User("admin", "Jan", "Novák", Hash("admin"), DateTime.Now, UserPower.admin, false).Save(pathToUsers);
                Console.WriteLine("Byl vytvořen adminský účet. (admin admin)");
            }

            if (!File.Exists(pathToVehicles))
            {
                SaveVehicleList(new List<Vehicle>());
                Console.WriteLine("Byl vytvořen prázdný soubor aut.");
            }

            if (File.Exists(pathToUsers + "checksum.dat"))
                EncryptedFile.CheckChecksum(pathToUsers);
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("CHECKSUM SOUBORU " + pathToUsers + " NEEXISTUJE.");
                Console.ForegroundColor = ConsoleColor.Gray;
            }

            if (File.Exists(pathToVehicles + "checksum.dat"))
                EncryptedFile.CheckChecksum(pathToVehicles);
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("CHECKSUM SOUBORU " + pathToVehicles + " NEEXISTUJE.");
                Console.ForegroundColor = ConsoleColor.Gray;
            }


            Console.WriteLine();
            Console.ReadLine();
            Console.Clear();


            MainMenu();
        }

        static void MainMenu()
        {
            Console.Clear();
            Console.WriteLine("*******************************************");
            Console.WriteLine("*************** Hlavní menu ***************");
            Console.WriteLine("*******************************************");

            if (loggedInUser != null)
            {
                if (loggedInUser.changePassword)
                {
                    ChangePassword();
                    return;
                }

                if (loggedInUser.power == UserPower.admin)
                {
                    Console.WriteLine("Přihlášen jako ADMIN " + loggedInUser.name + " " + loggedInUser.surname);
                    Console.WriteLine();
                    MenuGenerator(new Action[] { new Action("Správa uživatelů", UserManagment),
                                                 new Action("Správa aut", CarManagment),
                                                 new Action("Změna hesla", ChangePassword),
                                                 new Action("Odhlášení uživatele", Logout),
                                                 new Action("Konec", ExitApplication),});
                }
                else
                {
                    Console.WriteLine("Přihlášen jako " + loggedInUser.name + " " + loggedInUser.surname);
                    Console.WriteLine();
                    MenuGenerator(new Action[] { new Action("Rezervace aut", VehicleReservation),
                                                 new Action("Změna hesla", ChangePassword),
                                                 new Action("Odhlášení uživatele", Logout),
                                                 new Action("Konec", ExitApplication),});
                }
            }
            else
            {
                Console.WriteLine("Uživatel není přihlášen.");
                Console.WriteLine();
                MenuGenerator(new Action[] { new Action("Přihlášení uživatele", Login),
                                             new Action("Konec", ExitApplication),});
            }
        }

        static void ExitApplication()
        {
            Environment.Exit(0);
        }

        static void MenuGenerator(Action[] actions)
        {
            Console.WriteLine("Co chcete udělat?");
            for (int i = 0; i < actions.Length; i++)
            {
                Console.WriteLine("     [" + i + "] " + actions[i].actionName);
            }
            Console.WriteLine();

            int actionID = ReadlineToInt(0, actions.Length - 1);
            actions[actionID].callback.Invoke();
        }


        #region Okna
        static void Registration()
        {
            string username;
            string name;
            string surname;
            string password;
            string password2;

            Console.Clear();
            Console.WriteLine("*******************************************");
            Console.WriteLine("********** Registrace uživatele  **********");
            Console.WriteLine("*******************************************");
            Console.WriteLine();

            while (true)
            {
                Console.WriteLine("Prosím zadejte své unikátní přihlašovací jméno:");
                username = ReadlineToString();

                if (User.UsernameIsUnique(username, pathToUsers))
                    break;

                Console.WriteLine("Přihlašovací jméno už někdo používá.");
                Console.WriteLine();
            }

            Console.WriteLine("Prosím zadejte své křestní jméno:");
            name = ReadlineToString();
            Console.WriteLine("Prosím zadejte své příjmení:");
            surname = ReadlineToString();

            while (true)
            {
                Console.WriteLine("Prosím zadejte své heslo:");
                password = ReadlineToString(8);
                Console.WriteLine("Prosím zadejte své heslo pro potvrzení:");
                password2 = ReadlineToString(8);

                if (password == password2)
                    break;

                Console.WriteLine("Hesla se neshodují.");
                Console.WriteLine();
            }

            new User(username, name, surname, Hash(password), DateTime.Now, UserPower.user, false).Save(pathToUsers);

            Console.WriteLine();
            Console.WriteLine("Jste úspěšně zaregistrován. Děkujeme že jste s námi.");
            Console.WriteLine();

            Console.WriteLine("[Stiskněte enter]");
            Console.ReadLine();
            UserManagment();
        }

        static void Login()
        {
            string username;
            string password;

            Console.Clear();
            Console.WriteLine("*******************************************");
            Console.WriteLine("********** Přihlášení uživatele  **********");
            Console.WriteLine("*******************************************");
            Console.WriteLine();

            while (true)
            {
                Console.WriteLine("Prosím zadejte své přihlašovací jméno:");
                username = ReadlineToString();
                Console.WriteLine("Prosím zadejte své heslo:");
                password = ReadlineToString();

                if (User.PasswordMatchesUsername(username, Hash(password), pathToUsers))
                    break;

                Console.WriteLine("Hesla se neshodují");
                Console.WriteLine();
            }

            User.UpdateLastLogin(username, pathToUsers);

            loggedInUser = User.GetUserByUsername(username, pathToUsers);

            Console.WriteLine();
            Console.WriteLine("Jste úspěšně přihlášen.");
            Console.WriteLine();

            Console.WriteLine("[Stiskněte enter]");
            Console.ReadLine();

            if (loggedInUser.changePassword)
            {
                ChangePassword();
            }
            else
            {
                MainMenu();
            }
        }

        static void ChangePassword()
        {
            string oldPassword;
            string password;
            string password2;

            Console.Clear();
            Console.WriteLine("*******************************************");
            Console.WriteLine("*************** Změna hesla ***************");
            Console.WriteLine("*******************************************");
            Console.WriteLine();

            while (true)
            {
                Console.WriteLine("Prosím zadejte své staré heslo:");
                oldPassword = ReadlineToString();
                Console.WriteLine("Prosím zadejte své nové heslo:");
                password = ReadlineToString(8);
                Console.WriteLine("Prosím zadejte své heslo pro potvrzení:");
                password2 = ReadlineToString(8);

                if (password == password2 && Hash(oldPassword) == loggedInUser.passwordHash && Hash(password) != Hash(oldPassword))
                    break;

                Console.WriteLine("Hesla se neshodují nebo je vaše staré heslo špatně.");
                Console.WriteLine();
            }
            loggedInUser.passwordHash = Hash(password);
            loggedInUser.changePassword = false;
            loggedInUser.Save(pathToUsers);

            Console.WriteLine();
            Console.WriteLine("Heslo úspěšně změněno.");
            Console.WriteLine();

            Console.WriteLine("[Stiskněte enter]");
            Console.ReadLine();
            MainMenu();
        }


        static void VehicleReservation()
        {
            Console.Clear();
            Console.WriteLine("*******************************************");
            Console.WriteLine("************** Rezervace aut **************");
            Console.WriteLine("*******************************************");
            Console.WriteLine();

            MenuGenerator(new Action[] { new Action("Seznam aut", VehicleListUser),
                                         new Action("Zaregistrovat auto", ReserveVehicle),
                                         new Action("Zrušit registraci auta", UnreserveVehicle),
                                         new Action("Zpět", Console.Clear),});

            MainMenu();
        }

        static void UserManagment()
        {
            Console.Clear();
            Console.WriteLine("*******************************************");
            Console.WriteLine("************ Správa uživatelů *************");
            Console.WriteLine("*******************************************");
            Console.WriteLine();

            MenuGenerator(new Action[] { new Action("Seznam uživatelů", UserList),
                                         new Action("Registrace uživatele", Registration),
                                         new Action("Odebrat uživatele", RemoveUser),
                                         new Action("Vynucení změny hesla", ForcePasswordChange),
                                         new Action("Zpět", Console.Clear)});
            MainMenu();
        }

        static void CarManagment()
        {
            Console.Clear();
            Console.WriteLine("*******************************************");
            Console.WriteLine("*************** Správa  aut ***************");
            Console.WriteLine("*******************************************");
            Console.WriteLine();

            MenuGenerator(new Action[] { new Action("Seznam aut", VehicleList),
                                         new Action("Seznam aut podle uživatele", VehicleListByUser),
                                         new Action("Přidat auto", AddVehicle),
                                         new Action("Odebrat auto", RemoveVehicle),
                                         new Action("Zpět", Console.Clear),});
            MainMenu();
        }


        static void VehicleList()
        {
            Console.Clear();
            Console.WriteLine("*******************************************");
            Console.WriteLine("*************** Seznam aut ****************");
            Console.WriteLine("*******************************************");
            Console.WriteLine();

            List<Vehicle> vehicles = Vehicle.ListOfVehicles(pathToVehicles);

            for (int i = 0; i < vehicles.Count; i++)
            {
                if (vehicles[i].ownerUsername == "")
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Auto nemá nikdo zaregistrované.");
                    Console.ForegroundColor = ConsoleColor.White;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Registrováno k: " + vehicles[i].ownerUsername);
                    Console.ForegroundColor = ConsoleColor.White;
                }
                Console.WriteLine(vehicles[i]);
                Console.WriteLine("Servis:         ");

                for (int j = 0; j < vehicles[i].vehicleServices.Count; j++)
                {
                    Console.WriteLine(vehicles[i].vehicleServices[j]);
                    Console.WriteLine();
                }
                Console.WriteLine("--------------------------------------------------------------");
            }

            Console.WriteLine("[Stiskněte enter]");
            Console.ReadLine();
            CarManagment();
        }

        static void VehicleListByUser()
        {
            Console.Clear();
            Console.WriteLine("*******************************************");
            Console.WriteLine("********** Seznam aut uživatele ***********");
            Console.WriteLine("*******************************************");
            Console.WriteLine();

            List<Vehicle> vehicles = Vehicle.ListOfVehicles(pathToVehicles);

            Console.WriteLine("Prosím zadejte přihlašovací jméno uživatele:");
            string username = ReadlineToString(0);
            if (username.Length != 0)
            {
                for (int i = 0; i < vehicles.Count; i++)
                {
                    if (vehicles[i].ownerUsername == username)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Registrováno k: " + vehicles[i].ownerUsername);
                        Console.ForegroundColor = ConsoleColor.White;

                        Console.WriteLine(vehicles[i]);
                        Console.WriteLine("Servis:         ");

                        for (int j = 0; j < vehicles[i].vehicleServices.Count; j++)
                        {
                            Console.WriteLine(vehicles[i].vehicleServices[j]);
                            Console.WriteLine();
                        }
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("--------------------------------------------------------------");
                    }
                }

                Console.WriteLine("[Stiskněte enter]");
                Console.ReadLine();
            }
            CarManagment();
        }

        static void VehicleListUser()
        {
            Console.Clear();
            Console.WriteLine("*******************************************");
            Console.WriteLine("*************** Seznam aut ****************");
            Console.WriteLine("*******************************************");
            Console.WriteLine();

            List<Vehicle> vehicles = Vehicle.ListOfVehicles(pathToVehicles);

            for (int i = 0; i < vehicles.Count; i++)
            {
                if (vehicles[i].ownerUsername == loggedInUser.username ||
                    vehicles[i].ownerUsername == "")
                {
                    if (vehicles[i].ownerUsername == loggedInUser.username)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Vámi zaregistrované auto:");
                    }
                    Console.WriteLine(vehicles[i]);
                    Console.WriteLine("Servis:         ");

                    for (int j = 0; j < vehicles[i].vehicleServices.Count; j++)
                    {
                        Console.WriteLine(vehicles[i].vehicleServices[j]);
                        Console.WriteLine();
                    }
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("--------------------------------------------------------------");
                }
            }

            Console.WriteLine("[Stiskněte enter]");
            Console.ReadLine();
            VehicleReservation();
        }

        static void AddVehicle()
        {
            Console.Clear();
            Console.WriteLine("*******************************************");
            Console.WriteLine("************** Přidání auta ***************");
            Console.WriteLine("*******************************************");
            Console.WriteLine();

            List<Vehicle> vehicles = Vehicle.ListOfVehicles(pathToVehicles);

            int id = 0;
            for (int i = 0; i < vehicles.Count; i++)
            {
                if (id <= vehicles[i].id)
                    id = vehicles[i].id + 1;
            }

            string ownerUsername = "";
            string brand;
            string model;
            VehicleType type;
            float consuption;
            List<VehicleService> vehicleServices = new List<VehicleService>();

            Console.WriteLine("Prosím zadejte značku auta:");
            brand = ReadlineToString();

            Console.WriteLine("Prosím zadejte model auta:");
            model = ReadlineToString();

            Console.WriteLine("Prosím zadejte typ auta: (car / van / truck / motorcycle / bus)");
            VehicleType vehicleType;
            while (true)
            {
                if (Enum.TryParse(Console.ReadLine(), out vehicleType))
                    break;
                else
                    Console.WriteLine("Prosím zadejte typ auta: (car / van / truck / motorcycle / bus)");
            }
            type = vehicleType;

            Console.WriteLine("Prosím zadejte spotřebu auta:");
            consuption = ReadlineToSingle();

            while (true)
            {
                string serviceName;
                DateTime time;
                float price;
                string invoiceNumber;

                Console.WriteLine("Chcete přidat další servis? (y / n)");
                if (Console.ReadLine() == "y")
                {
                    Console.WriteLine("Prosím zadejte jméno servisu:");
                    serviceName = ReadlineToString();

                    Console.WriteLine("Prosím zadejte čas servisu: (dd.mm.yyyy hh:mm:ss)");
                    time = ReadlineToDatetime();

                    Console.WriteLine("Prosím zadejte cenu servisu:");
                    price = ReadlineToSingle();

                    Console.WriteLine("Prosím zadejte číslo faktury:");
                    invoiceNumber = ReadlineToString();

                    Console.WriteLine();
                    vehicleServices.Add(new VehicleService(serviceName, time, price, invoiceNumber));
                }
                else
                    break;
            }

            vehicles.Add(new Vehicle(id, ownerUsername, brand, model, type, consuption, vehicleServices));
            SaveVehicleList(vehicles);

            Console.WriteLine("[Stiskněte enter]");
            Console.ReadLine();
            CarManagment();
        }

        static void RemoveVehicle()
        {
            Console.Clear();
            Console.WriteLine("*******************************************");
            Console.WriteLine("************** Odebrání auta **************");
            Console.WriteLine("*******************************************");
            Console.WriteLine();

            List<Vehicle> vehicles = Vehicle.ListOfVehicles(pathToVehicles);

            Console.WriteLine("Prosím zadejte ID auta k vymazání: (-1 pro vrácení se)");
            int vehicleID = ReadlineToInt(-1);
            if (vehicleID != -1)
            {
                vehicles.Remove(vehicles.Find(i => i.id == vehicleID));
                SaveVehicleList(vehicles);

                Console.WriteLine("Auto bylo smazáno.");
                Console.WriteLine();

                Console.WriteLine("[Stiskněte enter]");
                Console.ReadLine();
            }
            CarManagment();
        }


        static void ReserveVehicle()
        {
            Console.Clear();
            Console.WriteLine("*******************************************");
            Console.WriteLine("************* Rezervace  auta *************");
            Console.WriteLine("*******************************************");
            Console.WriteLine();

            List<Vehicle> vehicles = Vehicle.ListOfVehicles(pathToVehicles);

            while (true)
            {
                Console.WriteLine("Prosím zadejte ID auta které si přejete zarezervovat:");
                int vehicleID = ReadlineToInt();
                if (vehicleID == -1)
                    break;

                if (vehicles.FindIndex(i => i.id == vehicleID) >= 0 && vehicles.Find(i => i.id == vehicleID).ownerUsername == "")
                {
                    Vehicle vehicle = vehicles.Find(i => i.id == vehicleID);
                    vehicle.ownerUsername = loggedInUser.username;
                    vehicle.Save(pathToVehicles);

                    Console.WriteLine("Auto bylo úspěšně zaregistrováno.");
                    Console.WriteLine();
                    Console.WriteLine("[Stiskněte enter]");
                    Console.ReadLine();
                    break;
                }
                else
                {
                    Console.WriteLine("Auto nebylo možno zaregistrovat. Přejete si pokračovat? (y / n)");
                    if (Console.ReadLine() != "y")
                    {
                        break;
                    }
                }
            }

            VehicleReservation();
        }

        static void UnreserveVehicle()
        {
            Console.Clear();
            Console.WriteLine("*******************************************");
            Console.WriteLine("********* Zrušení registrace auta *********");
            Console.WriteLine("*******************************************");
            Console.WriteLine();

            List<Vehicle> vehicles = Vehicle.ListOfVehicles(pathToVehicles);

            while (true)
            {
                Console.WriteLine("Prosím zadejte ID auta, u kterého si přejete zrušit registraci:");
                int vehicleID = ReadlineToInt();
                if (vehicles.FindIndex(i => i.id == vehicleID) >= 0 && vehicles.Find(i => i.id == vehicleID).ownerUsername == loggedInUser.username)
                {
                    Vehicle vehicle = vehicles.Find(i => i.id == vehicleID);
                    vehicle.ownerUsername = "";
                    vehicle.Save(pathToVehicles);

                    Console.WriteLine("Auto již nemáte zaregistrované.");
                    Console.WriteLine();
                    Console.WriteLine("[Stiskněte enter]");
                    Console.ReadLine();
                    break;
                }
                else
                {
                    Console.WriteLine("Autu nebylo možné zrušit registraci. Přejete si pokračovat? (y / n)");
                    if (Console.ReadLine() != "y")
                    {
                        break;
                    }
                }
            }

            VehicleReservation();
        }


        static void UserList()
        {
            Console.Clear();
            Console.WriteLine("*******************************************");
            Console.WriteLine("************ Seznam uživatelů *************");
            Console.WriteLine("*******************************************");
            Console.WriteLine();

            List<User> users = User.ListOfUsers(pathToUsers);

            for (int i = 0; i < users.Count; i++)
            {
                Console.WriteLine(users[i].ToString());
                Console.WriteLine("--------------------------------------------------------------");
            }

            Console.WriteLine("[Stiskněte enter]");
            Console.ReadLine();
            UserManagment();
        }

        static void RemoveUser()
        {
            Console.Clear();
            Console.WriteLine("*******************************************");
            Console.WriteLine("*********** Odebrání  uživatele ***********");
            Console.WriteLine("*******************************************");
            Console.WriteLine();

            List<User> users = User.ListOfUsers(pathToUsers);
            List<Vehicle> vehicles = Vehicle.ListOfVehicles(pathToVehicles);

            Console.WriteLine("Prosím zadejte přihlašovací jméno uživatele k vymazání:");
            string username = ReadlineToString(0);

            if (username.Length != 0)
            {
                if (users.Find(i => i.username == username).power == UserPower.admin)
                {
                    Console.WriteLine("Uživatel nemůže být smazán.");
                }
                else
                {
                    users.Remove(users.Find(i => i.username == username));
                    while (vehicles.Find(i => i.ownerUsername == username) != null)
                    {
                        vehicles.Find(i => i.ownerUsername == username).ownerUsername = "";
                    }
                    SaveUserList(users);
                    SaveVehicleList(vehicles);
                    Console.WriteLine("Uživatel byl smazán.");
                }

                Console.WriteLine();
                Console.WriteLine("[Stiskněte enter]");
                Console.ReadLine();
            }

            UserManagment();
        }

        static void ForcePasswordChange()
        {
            Console.Clear();
            Console.WriteLine("*******************************************");
            Console.WriteLine("*********** Vynucení změny hesla **********");
            Console.WriteLine("*******************************************");
            Console.WriteLine();

            List<User> users = User.ListOfUsers(pathToUsers);

            Console.WriteLine("Prosím zadejte přihlašovací jméno uživatele, kterému chcete vynutit změnu hesla:");
            string username = ReadlineToString(0);
            if (username.Length != 0)
            {
                users.Find(i => i.username == username).changePassword = true;
                SaveUserList(users);
                Console.WriteLine("Uživatel si při příštím přihlášení bude muset změnit heslo.");
                Console.WriteLine();

                Console.WriteLine("[Stiskněte enter]");
                Console.ReadLine();
            }
            UserManagment();
        }
        #endregion


        static void Logout()
        {
            loggedInUser = null;
            MainMenu();
        }

        static void SaveUserList(List<User> users)
        {
            List<string> file = new List<string>();
            for (int i = 0; i < users.Count; i++)
            {
                file.Add(users[i].username + "|" + users[i].name + "|" + users[i].surname + "|" + users[i].passwordHash + "|" + users[i].lastLogin + "|" + users[i].power + "|" + users[i].changePassword);
            }
            EncryptedFile.WriteAllLines(pathToUsers, file.ToArray());
        }

        static void SaveVehicleList(List<Vehicle> vehicles)
        {
            List<string> file = new List<string>();
            for (int i = 0; i < vehicles.Count; i++)
            {
                string services = "";
                for (int j = 0; j < vehicles[i].vehicleServices.Count; j++)
                {
                    services += vehicles[i].vehicleServices[j].serviceName + "*" + vehicles[i].vehicleServices[j].time + "*" + vehicles[i].vehicleServices[j].price + "*" + vehicles[i].vehicleServices[j].invoiceNumber + "/";
                }
                file.Add(vehicles[i].id + "|" + vehicles[i].ownerUsername + "|" + vehicles[i].brand + "|" + vehicles[i].model + "|" + vehicles[i].type + "|" + vehicles[i].consuption + "|" + services);
            }
            EncryptedFile.WriteAllLines(pathToVehicles, file.ToArray());
        }


        static string Hash(string data)
        {
            SHA256 sha256 = SHA256.Create();
            byte[] resultHash = sha256.ComputeHash(StringToByte(data));

            return HexByteToString(resultHash);
        }

        public static string HexByteToString(byte[] array)
        {
            string output = "";
            for (int i = 0; i < array.Length; i++)
            {
                output += $"{array[i]:X2}";
            }
            return output;
        }

        public static byte[] StringToByte(string data)
        {
            return Encoding.GetEncoding("UTF-16").GetBytes(data);
        }


        public static int ReadlineToInt(int min = Int32.MinValue, int max = Int32.MaxValue)
        {
            int output;
            while (true)
            {
                if (Int32.TryParse(Console.ReadLine(), out output))
                    if (output >= min && output <= max)
                        break;
                    else
                        Console.WriteLine("Zadejte prosím platné celé číslo v rozsahu " + min + " až " + max);
                else
                    Console.WriteLine("Zadejte prosím platné celé číslo.");
            }
            return output;
        }

        public static float ReadlineToSingle(float min = Single.MinValue, float max = Single.MaxValue)
        {
            float output;
            while (true)
            {
                if (Single.TryParse(Console.ReadLine(), out output))
                    if (output >= min && output <= max)
                        break;
                    else
                        Console.WriteLine("Zadejte prosím platné číslo v rozsahu " + min + " až " + max);
                else
                    Console.WriteLine("Zadejte prosím platné číslo.");
            }
            return output;
        }

        public static DateTime ReadlineToDatetime()
        {
            DateTime output;
            while (true)
            {
                if (DateTime.TryParse(Console.ReadLine(), out output))
                    break;
                else
                    Console.WriteLine("Zadejte prosím platné číslo.");
            }
            return output;
        }

        public static string ReadlineToString(int minLength = 1)
        {
            string output;
            while (true)
            {
                output = Console.ReadLine();
                if (output.Length >= minLength)
                    break;
                else
                    Console.WriteLine("Délka musí být minimálně " + minLength + ".");
            }
            return output;
        }
    }

    public delegate void Callback();

    class Action
    {
        public string actionName;
        public Callback callback;

        public Action(string actionName, Callback callback)
        {
            this.actionName = actionName;
            this.callback = callback;
        }
    }

    class User
    {
        public string username;
        public string name;
        public string surname;
        public string passwordHash;
        public DateTime lastLogin;
        public UserPower power;
        public bool changePassword;

        public User(string username, string name, string surname, string passwordHash, DateTime lastLogin, UserPower power, bool changePassword)
        {
            this.username = username;
            this.name = name;
            this.surname = surname;
            this.passwordHash = passwordHash;
            this.lastLogin = lastLogin;
            this.power = power;
            this.changePassword = changePassword;
        }

        public void Save(string pathToUsers)
        {
            List<string> file;
            if (!File.Exists(pathToUsers))
                file = new List<string>();
            else
                file = EncryptedFile.ReadAllLines(pathToUsers).ToList();

            for (int i = 0; i < file.Count; i++)
            {
                if (file[i].Split('|')[0] == username)
                {
                    file[i] = username + "|" + name + "|" + surname + "|" + passwordHash + "|" + lastLogin + "|" + power + "|" + changePassword;
                    EncryptedFile.WriteAllLines(pathToUsers, file.ToArray());
                    return;
                }
            }
            file.Add(username + "|" + name + "|" + surname + "|" + passwordHash + "|" + lastLogin + "|" + power + "|" + changePassword);
            EncryptedFile.WriteAllLines(pathToUsers, file.ToArray());
        }

        public static List<User> ListOfUsers(string pathToUsers)
        {
            List<User> users = new List<User>();
            string[] file = EncryptedFile.ReadAllLines(pathToUsers);
            for (int i = 0; i < file.Length; i++)
            {
                Enum.TryParse(file[i].Split('|')[5], out UserPower userPower);
                users.Add(new User(file[i].Split('|')[0],
                                   file[i].Split('|')[1],
                                   file[i].Split('|')[2],
                                   file[i].Split('|')[3],
                                   Convert.ToDateTime(file[i].Split('|')[4]),
                                   userPower,
                                   Convert.ToBoolean(file[i].Split('|')[6])));
            }
            return users;
        }

        public static User GetUserByUsername(string username, string pathToUsers)
        {
            string[] file = EncryptedFile.ReadAllLines(pathToUsers);
            for (int i = 0; i < file.Length; i++)
            {
                if (file[i].Split('|')[0] == username)
                {
                    Enum.TryParse(file[i].Split('|')[5], out UserPower userPower);
                    return new User(file[i].Split('|')[0],
                                    file[i].Split('|')[1],
                                    file[i].Split('|')[2],
                                    file[i].Split('|')[3],
                                    Convert.ToDateTime(file[i].Split('|')[4]),
                                    userPower,
                                    Convert.ToBoolean(file[i].Split('|')[6]));
                }
            }
            return null;
        }

        public static void UpdateLastLogin(string username, string pathToUsers)
        {
            User tmp = GetUserByUsername(username, pathToUsers);
            tmp.lastLogin = DateTime.Now;
            tmp.Save(pathToUsers);
        }

        public static bool UsernameIsUnique(string username, string pathToUsers)
        {
            List<User> list = ListOfUsers(pathToUsers);
            if (list.Find(i => i.username == username) != null)
                return false;
            else
                return true;
        }

        public static bool PasswordMatchesUsername(string username, string hashedPassword, string pathToUsers)
        {
            List<User> list = ListOfUsers(pathToUsers);
            if (list.Find(i => i.username == username) != null)
                if (list.Find(i => i.username == username).passwordHash == hashedPassword)
                    return true;
            return false;
        }

        public override string ToString()
        {
            return "Přihlašovací jméno:  " + username +
                 "\nKřestní jméno:       " + name +
                 "\nPříjmení:            " + surname +
                 "\nHash hesla:          " + passwordHash +
                 "\nPoslední přihlášení: " + lastLogin +
                 "\nSíla uživatele:      " + power +
                 "\nNutná změna hesla:   " + changePassword;
        }
    }

    enum UserPower
    {
        guest,
        user,
        admin
    }

    class Vehicle
    {
        public int id;
        public string ownerUsername;
        public string brand;
        public string model;
        public VehicleType type;
        public float consuption;
        public List<VehicleService> vehicleServices;

        public Vehicle(int id, string ownerUsername, string brand, string model, VehicleType type, float consuption, List<VehicleService> vehicleServices)
        {
            this.id = id;
            this.ownerUsername = ownerUsername;
            this.brand = brand;
            this.model = model;
            this.type = type;
            this.consuption = consuption;
            this.vehicleServices = vehicleServices;
        }

        public void Save(string pathToVehicles)
        {
            string[] file = EncryptedFile.ReadAllLines(pathToVehicles);
            for (int i = 0; i < file.Length; i++)
            {
                if (Convert.ToInt32(file[i].Split('|')[0]) == id)
                {
                    string services = "";
                    for (int j = 0; j < vehicleServices.Count; j++)
                    {
                        services += vehicleServices[j].serviceName + "*" + vehicleServices[j].time + "*" + vehicleServices[j].price + "*" + vehicleServices[j].invoiceNumber + "/";
                    }
                    file[i] = id + "|" + ownerUsername + "|" + brand + "|" + model + "|" + type + "|" + consuption + "|" + services;
                }
            }
            EncryptedFile.WriteAllLines(pathToVehicles, file);
        }

        public static Vehicle VehicleFromString(string text)
        {
            string[] vehicleSplit = text.Split('|');

            Enum.TryParse(vehicleSplit[4], out VehicleType vehicleType);

            List<VehicleService> vehicleServices = new List<VehicleService>();
            string[] serviceSplit = vehicleSplit[6].Split('/');
            for (int i = 0; i < serviceSplit.Length; i++)
            {
                string[] serviceDeepSplit = serviceSplit[i].Split('*');

                if (serviceDeepSplit.Length > 1)
                    vehicleServices.Add(new VehicleService(serviceDeepSplit[0],
                                                           Convert.ToDateTime(serviceDeepSplit[1]),
                                                           Convert.ToSingle(serviceDeepSplit[2]),
                                                           serviceDeepSplit[3]));
            }

            return new Vehicle(Convert.ToInt32(vehicleSplit[0]),
                               vehicleSplit[1],
                               vehicleSplit[2],
                               vehicleSplit[3],
                               vehicleType,
                               Convert.ToSingle(vehicleSplit[5]),
                               vehicleServices);
        }

        public static List<Vehicle> ListOfVehicles(string pathToVehicles)
        {
            List<Vehicle> vehicles = new List<Vehicle>();
            string[] file = EncryptedFile.ReadAllLines(pathToVehicles);
            if (file[0].Length > 0)
                for (int i = 0; i < file.Length; i++)
                {
                    vehicles.Add(Vehicle.VehicleFromString(file[i]));
                }
            return vehicles;
        }

        public override string ToString()
        {
            return "ID:             " + id +
                 "\nZnačka:         " + brand +
                 "\nModel:          " + model +
                 "\nTyp:            " + type +
                 "\nSpotřeba:       " + consuption;
        }
    }

    enum VehicleType
    {
        car,
        van,
        truck,
        motorcycle,
        bus,
    }

    class VehicleService
    {
        public string serviceName;
        public DateTime time;
        public float price;
        public string invoiceNumber;

        public VehicleService(string serviceName, DateTime time, float price, string invoiceNumber)
        {
            this.serviceName = serviceName;
            this.time = time;
            this.price = price;
            this.invoiceNumber = invoiceNumber;
        }

        public override string ToString()
        {
            return "    Jméno:         " + serviceName +
                 "\n    Čas provedení: " + time +
                 "\n    Cena:          " + price + " CZK" +
                 "\n    Číslo faktury: " + invoiceNumber;
        }
    }

    class EncryptedFile
    {
        static byte[] key = StringToByte("05C0D7850D415FE3F0DFDA4BC8515B114C7F6910102FAABF461CA8289469911F");
        static byte[] iv = StringToByte("2B52B0894C7418B291676DBE2B1220BB");

        public static string[] ReadAllLines(string path)
        {
            if (File.ReadAllText(path) == "")
                return new string[] { "" };
            byte[] data = Decrypt(File.ReadAllBytes(path), key, iv);
            string dataString = Encoding.GetEncoding("UTF-16").GetString(data);
            string[] output = dataString.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            return output;
        }

        public static void WriteAllLines(string path, string[] data)
        {
            string stringData = "";
            for (int i = 0; i < data.Length; i++)
            {
                stringData += data[i];
                if (i < data.Length - 1)
                    stringData += "\n";
            }

            File.WriteAllBytes(path, Encrypt(Encoding.GetEncoding("UTF-16").GetBytes(stringData), key, iv));
            SaveChecksum(path);
        }

        static void SaveChecksum(string path)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(path))
                {
                    File.WriteAllBytes(path + "checksum.dat", md5.ComputeHash(stream));
                }
            }
        }

        public static void CheckChecksum(string path)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(path))
                {
                    var checksum = md5.ComputeHash(stream);
                    var saveCheck = File.ReadAllBytes(path + "checksum.dat");

                    if (Encoding.ASCII.GetString(checksum) != Encoding.ASCII.GetString(saveCheck))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("CHECKSUM SOUBORU " + path + " NEODPOVÍDÁ TOMU ULOŽENÉMU.");
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                }
            }
        }

        static byte[] Encrypt(byte[] data, byte[] key, byte[] iv)
        {
            using (var aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Key = key;
                aes.IV = iv;

                using (var cryptoTransform = aes.CreateEncryptor())
                {
                    return cryptoTransform.TransformFinalBlock(data, 0, data.Length);
                }
            }
        }

        static byte[] Decrypt(byte[] data, byte[] key, byte[] iv)
        {
            using (var aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Key = key;
                aes.IV = iv;

                using (var cryptoTransform = aes.CreateDecryptor())
                {
                    return cryptoTransform.TransformFinalBlock(data, 0, data.Length);
                }
            }
        }

        public static byte[] StringToByte(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public static string ByteToString(byte[] array)
        {
            string output = "";
            for (int i = 0; i < array.Length; i++)
            {
                output += $"{array[i]:X2}";
            }

            return output;
        }
    }
}
