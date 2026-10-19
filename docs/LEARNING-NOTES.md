# Бележки по ученето (C# и .NET)

Кратки обяснения на понятията, които се срещат във всеки етап.

## Етап 1: solution, API и тестов проект

- **Solution (`.slnx`)** групира проекти. Не съдържа код. `dotnet build` и `dotnet test` работят върху всички проекти наведнъж.
- **Project (`.csproj`)** е един компилируем модул с настройки (версия на .NET, пакети).
- **ProjectReference** позволява на тестовия проект да ползва кода на API проекта.
- **Minimal API**: endpoint-ите се пишат в `Program.cs` с `app.MapGet(...)`, без контролер класове.
- **`record`** е кратък тип за данни (шаблонът го ползва за `WeatherForecast`).

## Етап 2: TaskItem и endpoint-и в паметта

- **`class` и свойства** (`{ get; set; }`): `TaskItem` описва задача. Свойствата са автоматични.
- **`enum`**: `TaskItemStatus` позволява само `Todo`, `InProgress`, `Done`.
- **Nullable reference types**: `string` не може да е `null`, `string?` може. Компилаторът ни предупреждава.
- **Интерфейс** (`ITaskStore`) е договор с методи без код. `InMemoryTaskStore` го изпълнява.
- **Dependency injection**: endpoint-ът казва `(ITaskStore store)`, а .NET му подава реализацията, регистрирана с `AddSingleton`.
- **Singleton**: един екземпляр за цялото приложение. Нужен е, защото списъкът живее в паметта.
- **`lock`**: заявките идват едновременно и `List<T>` не е безопасен за паралелен достъп. `lock` пуска по една нишка в блока.
- **LINQ и лямбда**: `FirstOrDefault(t => t.Id == id)` връща първата съвпадаща задача или `null`.
- **HTTP кодове**: `201 Created` (с `Location` хедър) при създаване, `200 OK` при четене, `404 Not Found` при липсваща задача.
- **`JsonStringEnumConverter`**: показва enum като дума (`"Todo"`), не като число.
- **`public partial class Program;`**: прави класа видим за тестовия проект (ползва се в етап 5).
