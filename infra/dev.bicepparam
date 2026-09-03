using './main.bicep'

param applicationName = 'agenticops'
param location = 'westeurope'
// Set during deployment; it must be globally unique.
param containerRegistryName = 'replaceatdeployment'
param apiImage = 'replaceatdeployment.azurecr.io/api:bootstrap'
param dashboardImage = 'replaceatdeployment.azurecr.io/dashboard:bootstrap'
