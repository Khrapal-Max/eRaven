# Domain & Infrastructure Test Execution Report

## Scope
- `test/eRaven.Tests/Domain.Tests`
- `test/eRaven.Tests/Application.Tests`

## Commands Attempted
```bash
cd /workspace/eRaven
 dotnet test test/eRaven.Tests/eRaven.Tests.csproj --filter FullyQualifiedName~Domain
 dotnet test test/eRaven.Tests/eRaven.Tests.csproj --filter FullyQualifiedName~Infrastructure
```

## Result
Тести не були виконані, оскільки в середовищі відсутній встановлений .NET SDK (`dotnet`). Команда `dotnet test` завершується помилкою `command not found`. Для запуску необхідно встановити відповідну версію .NET SDK (наприклад, 8.x) або виконати команди у середовищі, де SDK доступний.

## Рекомендації
1. Встановити .NET SDK та повторно виконати команди вище.
2. Або скористатися локальним середовищем розробки/CI, де .NET SDK вже присутній, для перевірки доменних та інфраструктурних тестів.
