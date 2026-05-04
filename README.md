# Industrial Processing System

## CI / Testovi / Code coverage

U repozitorijumu je podešen GitHub Actions workflow:

- `.github/workflows/ci.yml`
- pokre?e se na svaki **push** i **pull request** na `main`
- radi: restore ? build ? test + code coverage

### Gde se vidi na GitHub-u?

1. Otvori repo na GitHub-u
2. Klikni tab **Actions**
3. Odaberi workflow **CI**
4. Otvori poslednji run:
   - vidiš logove build/test
   - u sekciji **Artifacts** postoje:
     - `test-results`
     - `coverage` (Cobertura XML)

### Lokalno pokretanje testova i coverage-a

- Testovi:
  - `dotnet test IndustrialProcessingSystem.sln`

- Coverage (Cobertura XML):
  - `dotnet test IndustrialProcessingSystem.sln --collect:"XPlat Code Coverage" --results-directory TestResults`

> Napomena: GitHub Actions upload-uje `coverage.cobertura.xml` iz `TestResults` foldera kao artifact.
