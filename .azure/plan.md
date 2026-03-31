# Azure Deployment Plan

> **Status:** Deployed

Generated: 2026-03-31

---

## 1. Project Overview

**Goal:** Prepare Contoso Bank SRE Agent Demo for Azure deployment — write Bicep IaC modules (Task 11), validate them (Task 11t), and create azd configuration (Task 12). No deployment — user will run `azd up` manually.

**Path:** Modernize Existing (app code exists, adding Azure infra + azd config)

---

## 2. Requirements

| Attribute | Value |
|-----------|-------|
| Classification | POC / Demo |
| Scale | Small |
| Budget | Cost-Optimized |
| **Subscription** | User-provided at deploy time (parameterized) |
| **Location** | eastus2 (default, parameterized) |

---

## 3. Components Detected

| Component | Type | Technology | Path |
|-----------|------|------------|------|
| contoso-bank | Web App (Razor Pages + API) | .NET 10 / ASP.NET Core | src/ContosoBank/ |

## Dependencies

| Component | Depends On | Type |
|-----------|-----------|------|
| contoso-bank | Azure SQL Database | Database |
| contoso-bank | Application Insights | Telemetry |
| contoso-bank | Managed Prometheus | Metrics |
| contoso-bank | Managed Grafana | Dashboards |

## Existing Infrastructure

| Item | Status |
|------|--------|
| azure.yaml | Not found — will create |
| infra/ | Not found — will create |
| Dockerfile | Found: src/ContosoBank/Dockerfile |

---

## 4. Recipe Selection

**Selected:** AZD (Bicep)

**Rationale:** Spec requires Bicep IaC with `azd up` one-command deployment. Project is Azure-only, single-service containerized app.

---

## 5. Architecture

**Stack:** Containers (Azure Container Apps)

### Service Mapping

| Component | Azure Service | SKU |
|-----------|---------------|-----|
| contoso-bank | Container Apps | Consumption |
| Database | Azure SQL Database | Basic |
| Container Hosting | Container Apps Environment | Consumption |
| Container Images | Container Registry | Basic |

### Supporting Services

| Service | Purpose | Bicep Module |
|---------|---------|-------------|
| Log Analytics Workspace | Centralized logging | monitoring.bicep |
| Application Insights | APM + telemetry | monitoring.bicep |
| Azure Monitor Workspace | Managed Prometheus | prometheus.bicep |
| Azure Managed Grafana | Dashboards + MCP endpoint | grafana.bicep |
| User-Assigned Managed Identity | Service-to-service auth + RBAC | identity.bicep |
| Alert Rules + Action Group | 10 pre-configured alerts (A1–A10) | alerts.bicep |

---

## 6. Provisioning Limit Checklist

> **Note:** User will not deploy during this session. Quota validation skipped — user will validate at deploy time with `azd up`.

| Resource Type | Number to Deploy |
|---------------|------------------|
| Microsoft.App/managedEnvironments | 1 |
| Microsoft.App/containerApps | 1 |
| Microsoft.ContainerRegistry/registries | 1 |
| Microsoft.Sql/servers | 1 |
| Microsoft.Sql/servers/databases | 1 |
| Microsoft.OperationalInsights/workspaces | 1 |
| Microsoft.Insights/components | 1 |
| Microsoft.Monitor/accounts | 1 |
| Microsoft.Dashboard/grafana | 1 |
| Microsoft.ManagedIdentity/userAssignedIdentities | 1 |
| Microsoft.Insights/metricAlerts | 2 |
| Microsoft.Insights/scheduledQueryRules | 8 |
| Microsoft.Insights/actionGroups | 1 |

**Status:** ⏭️ Skipped — no deployment in this session

---

## 7. Execution Checklist

### Phase 1: Planning
- [x] Analyze workspace
- [x] Gather requirements
- [x] Confirm subscription and location with user
- [x] Prepare resource inventory
- [x] Scan codebase
- [x] Select recipe (AZD + Bicep)
- [x] Plan architecture
- [ ] **User approved this plan**

### Phase 2: Execution
- [x] Generate `azure.yaml` (Task 12)
- [x] Generate `infra/main.bicep` entry point (Task 11)
- [x] Generate `infra/main.bicepparam` parameters (Task 11)
- [x] Generate `infra/modules/identity.bicep` (Task 11)
- [x] Generate `infra/modules/monitoring.bicep` (Task 11)
- [x] Generate `infra/modules/container-env.bicep` (Task 11)
- [x] Generate `infra/modules/sql.bicep` (Task 11)
- [x] Generate `infra/modules/prometheus.bicep` (Task 11)
- [x] Generate `infra/modules/grafana.bicep` (Task 11)
- [x] Generate `infra/modules/container-app.bicep` (Task 11)
- [x] Generate `infra/modules/alerts.bicep` with A1–A10 (Task 11)
- [x] Run `az bicep build` validation (Task 11t)
- [x] Update plan status to "Ready for Validation"

### Phase 3: Validation
- [x] Invoke azure-validate skill
- [x] All validation checks pass (azd v1.23.13, Bicep compiled, provision preview OK, build OK, package OK)
- [x] Update plan status to "Validated"

### Phase 4: Deployment
- [ ] User runs `azd up` manually

---

## 7b. Validation Proof

| Check | Command | Result |
|-------|---------|--------|
| AZD installed | `azd version` | ✅ v1.23.13 |
| Auth | `azd auth login --check-status` | ✅ Logged in |
| Environment | `azd env new contoso-bank-sre` | ✅ Created |
| Bicep (main + 8 modules + params) | `az bicep build` | ✅ All pass |
| Provision preview | `azd provision --preview --no-prompt` | ✅ 7 resources |
| .NET build | `dotnet build` | ✅ 0 errors |
| Package | `azd package --no-prompt` | ✅ Success |

---

## 8. Files to Generate

| File | Purpose | Status |
|------|---------|--------|
| `.azure/plan.md` | This plan | ✅ |
| `azure.yaml` | AZD project definition + hooks | ✅ |
| `infra/main.bicep` | Subscription-scoped entry point | ✅ |
| `infra/main.bicepparam` | Parameter defaults | ✅ |
| `infra/modules/identity.bicep` | Managed Identity + RBAC | ✅ |
| `infra/modules/monitoring.bicep` | App Insights + Log Analytics | ✅ |
| `infra/modules/container-env.bicep` | Container Apps Environment + ACR | ✅ |
| `infra/modules/sql.bicep` | Azure SQL Server + Database | ✅ |
| `infra/modules/prometheus.bicep` | Monitor Workspace + DCR | ✅ |
| `infra/modules/grafana.bicep` | Managed Grafana + RBAC | ✅ |
| `infra/modules/container-app.bicep` | Container App | ✅ |
| `infra/modules/alerts.bicep` | 10 alert rules + action group | ✅ |
| `scripts/post-provision.sh` | Post-provision hook (MI SQL grant) | ✅ |

---

## 9. Next Steps

> Current: Validated — ready for `azd up`

1. Run `azd up` to deploy everything
2. Post-provision script grants managed identity SQL access
3. Grafana MCP endpoint URL printed after provisioning
