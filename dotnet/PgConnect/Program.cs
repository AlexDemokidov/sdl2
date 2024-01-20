/////////////////////Pinger///////////////////
using Npgsql;
using System;

// Переменные окружения
string intervalStr = Environment.GetEnvironmentVariable("INTERVAL");
string username = Environment.GetEnvironmentVariable("USERNAME");
string password = Environment.GetEnvironmentVariable("PASSWORD");

string logFilePath = Environment.GetEnvironmentVariable("LOG_FILE_PATH");

// Парсинг значения интервала (в секундах)
double interval;
if (!double.TryParse(intervalStr, out interval))
{
    interval = 300; // Значение по умолчанию, если интервал не удалось распарсить
}

// Чтение конфигурационного файла
StreamReader sr = new StreamReader("sdl1.conf");
var connectionStringFromConf = sr.ReadLine();

Npgsql.NpgsqlConnectionStringBuilder csb = new Npgsql.NpgsqlConnectionStringBuilder(connectionStringFromConf);

// Замена в строке подключения пользователя и пароля
csb.Username = username;
csb.Password = password;

string connectionString = csb.ToString();

var tasklist = new List<Task>();

while (true)
{

    lock (tasklist)
    {
        tasklist.RemoveAll(x => x.IsCompleted || x.IsFaulted);

        if (tasklist.Count > 0)
        {
            using (StreamWriter logWriter = new StreamWriter(logFilePath, true))
            {
                // Сообщение записывается в файл
                logWriter.WriteLine($"{DateTime.Now}:Task is paused");
            }
            // Выводит сообщение в stdout
            Console.WriteLine($"{DateTime.Now}:Task is paused");
        }
        tasklist.Clear();

        var task = Task.Run(() =>
            {
                DoHeartbeat(connectionString);
            });

        tasklist.Add(task);

    }

    await Task.Delay(TimeSpan.FromSeconds(interval));
}

void DoHeartbeat(object state)
{
    string connectionString = (string)state;

    try
    {
        // Открытие подключения к БД
        using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
        {
            connection.Open();

            // Команда полкчение версии БД
            using (NpgsqlCommand command = new NpgsqlCommand("SELECT version()", connection))
            {
                // Результат записывается в переменную
                string version = (string)command.ExecuteScalar();

                using (StreamWriter logWriter = new StreamWriter(logFilePath, true))
                {
                    // Сообщение записывается в файл
                    logWriter.WriteLine($"{DateTime.Now}:PostgreSQL version: " + version);
                }
                // Выводит сообщение в stdout
                Console.WriteLine($"{DateTime.Now}:PostgreSQL version: " + version);
            }
        }
    }
    catch (Exception ex)
    {
        using (StreamWriter logWriter = new StreamWriter(logFilePath, true))
        {
            // Сообщение записывается в файл
            logWriter.WriteLine($"{DateTime.Now}:Error: " + ex.Message);
        }
        // Выводит ошибку в stderr
        Console.Error.WriteLine($"{DateTime.Now}:Error: " + ex.Message);
    }

}