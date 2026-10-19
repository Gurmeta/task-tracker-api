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

## Етап 3: пълен CRUD, валидация и HTTP кодове

- **DTO** (`CreateTaskRequest`, `UpdateTaskRequest`): отделни типове за входа. Клиентът не може да подава `Id` или `CreatedAt`. Това предпазва от *over-posting*.
- **`record` с `init`**: свойства, които се задават само при създаване на обекта.
- **DataAnnotations**: атрибути като `[Required]` и `[StringLength(100)]` описват правилата. `Validator.TryValidateObject` ги изпълнява.
- **`TaskItemStatus?` + `[Required]`**: без `?` липсващ статус би станал тихо `Todo` (стойност по подразбиране на enum). С `?` липсата е грешка.
- **`Results.ValidationProblem`**: връща `400` във формат *Problem Details* (стандарт RFC 9457) със списък на грешките по полета.
- **HTTP кодове**: `201` + `Location` (създаване), `200` (четене и промяна), `204 No Content` (успешно изтриване), `400` (невалидни данни), `404` (липсва ресурс).
- **`PUT` е пълна замяна**: клиентът праща всички полета. (`PATCH` е частична промяна, не го правим сега.)
- **`MapGroup("/tasks")`**: общ префикс за група endpoint-и. Кодът е в отделен клас с **extension method** (`this IEndpointRouteBuilder`), за да е `Program.cs` кратък.
- **Неизменяеми обекти в хранилището**: `Update` заменя обекта с нов, вместо да го променя. Така обект, който друга заявка чете, не се променя по средата.
- **Параметри на заявката**: `TaskItemStatus? status` се чете от `?status=Done`. Невалидна стойност дава автоматично `400`.
- **`JsonStringEnumConverter(allowIntegerValues: false)`**: числа като `"status": 1` се отхвърлят.
