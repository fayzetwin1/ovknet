SOLUTION := Ovk.Net.slnx
WEB_PROJECT := Ovk.Net.Web/Ovk.Net.Web.csproj
HTTP_PROFILE := http
HTTPS_PROFILE := https
HTTP_URL := http://localhost:5038
HTTPS_URL := https://localhost:7048

.DEFAULT_GOAL := help

.PHONY: help restore build build-web run run-https watch watch-https clean certs urls

help:
	@echo "Ovk.Net development commands:"
	@echo "  make restore      Restore NuGet packages"
	@echo "  make build        Build the whole solution"
	@echo "  make build-web    Build only the web project"
	@echo "  make run          Run web server on $(HTTP_URL)"
	@echo "  make run-https    Run web server on $(HTTPS_URL) and $(HTTP_URL)"
	@echo "  make watch        Run web server with hot reload on $(HTTP_URL)"
	@echo "  make watch-https  Run web server with hot reload using HTTPS profile"
	@echo "  make clean        Clean build outputs"
	@echo "  make certs        Trust ASP.NET Core HTTPS development certificate"
	@echo "  make urls         Print configured local URLs"

restore:
	dotnet restore "$(SOLUTION)"

build:
	dotnet build "$(SOLUTION)"

build-web:
	dotnet build "$(WEB_PROJECT)"

run:
	dotnet run --project "$(WEB_PROJECT)" --launch-profile "$(HTTP_PROFILE)"

run-https:
	dotnet run --project "$(WEB_PROJECT)" --launch-profile "$(HTTPS_PROFILE)"

watch:
	dotnet watch --project "$(WEB_PROJECT)" run --launch-profile "$(HTTP_PROFILE)"

watch-https:
	dotnet watch --project "$(WEB_PROJECT)" run --launch-profile "$(HTTPS_PROFILE)"

clean:
	dotnet clean "$(SOLUTION)"

certs:
	dotnet dev-certs https --trust

urls:
	@echo "$(HTTP_URL)"
	@echo "$(HTTPS_URL)"
