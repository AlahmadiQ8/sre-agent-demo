// Azure Monitor Alert Rules (A1–A10) + Action Group
// 10 pre-configured alerts mapped to all 8 chaos scenarios
// Low thresholds for demo speed — alerts fire within 1–2 minutes

param name string
param location string = resourceGroup().location
param tags object = {}
param containerAppId string
param containerAppName string
param appInsightsId string
param logAnalyticsWorkspaceId string

// Action Group for all alerts
resource actionGroup 'Microsoft.Insights/actionGroups@2023-01-01' = {
  name: 'contosobank-alerts-ag'
  location: 'global'
  tags: tags
  properties: {
    enabled: true
    groupShortName: 'cb-alerts'
  }
}

// ============================================================
// Metric Alerts (A1, A3) — target Container App resource directly
// ============================================================

// A1: High Memory Usage — Scenario 1: Memory Leak
resource a1HighMemory 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: 'A1-High-Memory-Usage-${name}'
  location: 'global'
  tags: tags
  properties: {
    severity: 2
    enabled: true
    scopes: [containerAppId]
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'HighMemory'
          metricName: 'WorkingSetBytes'
          metricNamespace: 'microsoft.app/containerapps'
          operator: 'GreaterThan'
          threshold: 594706704 // ~600MB
          timeAggregation: 'Average'
          criterionType: 'StaticThresholdCriterion'
        }
      ]
    }
    autoMitigate: true
    actions: [
      {
        actionGroupId: actionGroup.id
      }
    ]
  }
}

// A3: High CPU Usage — Scenario 2: CPU Spike
resource a3HighCpu 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: 'A3-High-CPU-Usage-${name}'
  location: 'global'
  tags: tags
  properties: {
    severity: 2
    enabled: true
    scopes: [containerAppId]
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'HighCpu'
          metricName: 'UsageNanoCores'
          metricNamespace: 'microsoft.app/containerapps'
          operator: 'GreaterThan'
          threshold: 500000000 // 50% of 1 core in nanocores
          timeAggregation: 'Average'
          criterionType: 'StaticThresholdCriterion'
        }
      ]
    }
    autoMitigate: true
    actions: [
      {
        actionGroupId: actionGroup.id
      }
    ]
  }
}

// ============================================================
// Scheduled Query Rules (A2, A4–A10) — KQL queries against
// App Insights or Log Analytics
// ============================================================

// A2: Container OOM Restart — Scenario 1: Memory Leak (OOM kill)
resource a2OomRestart 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = {
  name: 'A2-Container-OOM-Restart-${name}'
  location: location
  tags: tags
  properties: {
    severity: 1
    enabled: true
    scopes: [logAnalyticsWorkspaceId]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT5M'
    criteria: {
      allOf: [
        {
          query: 'ContainerAppSystemLogs | where Reason == "OOMKilled"'
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 0
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    autoMitigate: true
    actions: {
      actionGroups: [actionGroup.id]
    }
  }
}

// A4: HTTP 5xx Error Spike — Scenario 3: HTTP 500 Errors
resource a4Http5xx 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = {
  name: 'A4-HTTP-5xx-Error-Spike-${name}'
  location: location
  tags: tags
  properties: {
    severity: 1
    enabled: true
    scopes: [appInsightsId]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT5M'
    criteria: {
      allOf: [
        {
          query: 'requests | where toint(resultCode) >= 500'
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 10
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    autoMitigate: true
    actions: {
      actionGroups: [actionGroup.id]
    }
  }
}

// A5: Database Dependency Failures — Scenario 4: DB Connection Failure
resource a5DbFailures 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = {
  name: 'A5-Database-Dependency-Failures-${name}'
  location: location
  tags: tags
  properties: {
    severity: 1
    enabled: true
    scopes: [appInsightsId]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT5M'
    criteria: {
      allOf: [
        {
          query: 'dependencies | where type == "SQL" and success == false'
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 5
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    autoMitigate: true
    actions: {
      actionGroups: [actionGroup.id]
    }
  }
}

// A6: High Request Latency (P95) — Scenario 5: Slow API
resource a6HighLatency 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = {
  name: 'A6-High-Request-Latency-P95-${name}'
  location: location
  tags: tags
  properties: {
    severity: 2
    enabled: true
    scopes: [appInsightsId]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT5M'
    criteria: {
      allOf: [
        {
          query: 'requests | summarize p95 = percentile(duration, 95) | where p95 > 10000'
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 0
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    autoMitigate: true
    actions: {
      actionGroups: [actionGroup.id]
    }
  }
}

// A7: Dependency Timeout Spike — Scenario 6: Dependency Timeout
resource a7DepTimeout 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = {
  name: 'A7-Dependency-Timeout-Spike-${name}'
  location: location
  tags: tags
  properties: {
    severity: 2
    enabled: true
    scopes: [appInsightsId]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT5M'
    criteria: {
      allOf: [
        {
          query: 'dependencies | where success == false and duration > 30000'
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 3
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    autoMitigate: true
    actions: {
      actionGroups: [actionGroup.id]
    }
  }
}

// A8: Abnormal Log Volume — Scenario 7: Log Flooding
resource a8LogVolume 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = {
  name: 'A8-Abnormal-Log-Volume-${name}'
  location: location
  tags: tags
  properties: {
    severity: 3
    enabled: true
    scopes: [logAnalyticsWorkspaceId]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT5M'
    criteria: {
      allOf: [
        {
          query: 'ContainerAppConsoleLogs | where ContainerAppName == \'${containerAppName}\''
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 5000
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    autoMitigate: true
    actions: {
      actionGroups: [actionGroup.id]
    }
  }
}

// A9: Exception Rate Spike — Scenario 8: Exception Storm
resource a9ExceptionSpike 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = {
  name: 'A9-Exception-Rate-Spike-${name}'
  location: location
  tags: tags
  properties: {
    severity: 1
    enabled: true
    scopes: [appInsightsId]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT5M'
    criteria: {
      allOf: [
        {
          query: 'exceptions'
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 50
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    autoMitigate: true
    actions: {
      actionGroups: [actionGroup.id]
    }
  }
}

// A10: Health Check Degraded — General (any severe scenario)
resource a10HealthDegraded 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = {
  name: 'A10-Health-Check-Degraded-${name}'
  location: location
  tags: tags
  properties: {
    severity: 1
    enabled: true
    scopes: [appInsightsId]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT5M'
    criteria: {
      allOf: [
        {
          query: 'requests | where name has "health" and toint(resultCode) != 200'
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 3
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    autoMitigate: true
    actions: {
      actionGroups: [actionGroup.id]
    }
  }
}
