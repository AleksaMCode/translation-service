SOLUTION=translator/translator.slnx
API_PROJECT=translator/translator/translator.csproj

.PHONY: help restore build run test test-coverage format format-check

# Restore NuGet packages
restore:
	dotnet restore $(SOLUTION)

build:
	dotnet build $(SOLUTION)

run:
	dotnet run --project $(API_PROJECT)

test:
	dotnet test $(SOLUTION)

test-coverage:
	dotnet test $(SOLUTION) --collect:"XPlat Code Coverage"

format:
	dotnet csharpier format .

format-check:
	dotnet csharpier check .
