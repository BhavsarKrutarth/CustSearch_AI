# CustsearchAdmin

CustSearch AI's shared Customer Admin and Platform Admin application, built with a custom semantic Light, Dark and System design system.

## Development server

Start the API HTTPS profile first, then use the project-local CLI:

```powershell
npm start
```

Open `http://localhost:4200/customer-admin` for Customer Admin or `http://localhost:4200/platform-admin` for Platform Admin. The development proxy sends `/api` requests to `https://localhost:7277`.

## Local/dev/UAT login testing

Do not guess or hardcode login passwords. Before each test run, inspect the latest `Users` and `Tenants` rows. Use the selected active user's `UserName` or `Email`, read its current `DisplayPassword` for local/dev/UAT only, and verify `IsActive = 1`. Tenant testing must use a matching `Users.TenantId = Tenants.Id`; platform testing uses the appropriate platform-scope account with `TenantId = NULL`. Never use another tenant accidentally, and never expose `DisplayPassword` through production APIs or UI.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```powershell
npm run build:production
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Vitest](https://vitest.dev/) test runner, use the following command:

```powershell
npm run test:ci
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
