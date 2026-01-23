using RedisPOC.Services;

namespace RedisPOC
{
    public class App
    {
        private readonly UserService _users;

        public App(UserService users) => _users = users;

        public async Task RunAsync(CancellationToken ct)
        {
            Guid? lastCreated = null;

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("==== MENU ====");
                Console.WriteLine("1) Create user (SQL)");
                Console.WriteLine("2) Get user by Id (Redis -> SQL)");
                Console.WriteLine("3) Get last created user");
                Console.WriteLine("4) Get user twice (prove cache)");
                Console.WriteLine("0) Exit");
                Console.Write("Choose: ");

                var choice = Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1":
                        {
                            var name = ReadRequiredString("Enter name: ");
                            var age = ReadIntInRange("Enter age: ", 0, 130);

                            var user = await _users.CreateAsync(name, age, ct);
                            lastCreated = user.Id;

                            Console.WriteLine($"Inserted into SQL: {user.Id}");
                            break;
                        }

                    case "2":
                        {
                            var id = ReadGuid("Enter user Id (GUID): ");
                            var (user, source) = await _users.GetAsync(id, ct);

                            if (user is null)
                                Console.WriteLine($"Not found (source={source})");
                            else
                            {
                                Console.WriteLine($"SOURCE={source}");
                                Console.WriteLine($"Id={user.Id}");
                                Console.WriteLine($"Name={user.Name}");
                                Console.WriteLine($"Age={user.Age}");
                                Console.WriteLine($"UpdatedAtUtc={user.UpdatedAtUtc:O}");
                            }
                            break;
                        }

                    case "3":
                        {
                            if (lastCreated is null)
                            {
                                Console.WriteLine("No user created in this run yet.");
                                break;
                            }

                            var (user, source) = await _users.GetAsync(lastCreated.Value, ct);
                            Console.WriteLine($"LastCreatedId={lastCreated.Value}");

                            if (user is null)
                                Console.WriteLine($"Not found (source={source})");
                            else
                                Console.WriteLine($"SOURCE={source} Name={user.Name} Age={user.Age}");

                            break;
                        }

                    case "4":
                        {
                            var id = ReadGuid("Enter user Id (GUID): ");

                            var (_, s1) = await _users.GetAsync(id, ct);
                            Console.WriteLine($"1st read SOURCE={s1}");

                            var (_, s2) = await _users.GetAsync(id, ct);
                            Console.WriteLine($"2nd read SOURCE={s2}");

                            break;
                        }

                    case "0":
                        return;

                    default:
                        Console.WriteLine("Invalid choice.");
                        break;
                }
            }
        }

        private static string ReadRequiredString(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                var s = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(s)) return s.Trim();
                Console.WriteLine("Value is required.");
            }
        }

        private static int ReadIntInRange(string prompt, int min, int max)
        {
            while (true)
            {
                Console.Write(prompt);
                var s = Console.ReadLine();
                if (int.TryParse(s, out var n) && n >= min && n <= max) return n;
                Console.WriteLine($"Enter a whole number between {min} and {max}.");
            }
        }

        private static Guid ReadGuid(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                var s = Console.ReadLine();
                if (Guid.TryParse(s, out var id)) return id;
                Console.WriteLine("Invalid GUID.");
            }
        }
    }
}
