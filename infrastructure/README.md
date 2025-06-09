# Infrastructure Documentation

This directory contains the Kubernetes and infrastructure configuration and provisioning files (using helm) for the multi-tenant application setup. The easiest way to provision the full environmnent in a local k3d cluster is using the Makefile which is present [here](../Makefile).

## Directory Overview

### Dockerfiles
- ``Dockerfile.azclikubectl`` - dockerfile containing azurecli and kubectl used in argo workflows
- ``Dockerfile.billing`` - dockerfile for the Billing Service (Control Plane)
- ``Dockerfile.hostedsite`` - dockerfile for the HostedSite Service (Application Plane)
- ``Dockerfile.manager`` - dockerfile for the Manager Service (Application Plane)
- ``Dockerfile.notificationservice`` - dockerfile for the Notification Service (Control Plane)
- ``Dockerfile.onboarding`` - dockerfile for the Onboarding Service (Control Plane)
- ``Dockerfile.selfprovision`` - dockerfile for the Self Provision Interface (Control Plane)
- ``Dockerfile.tenantmanagement`` - Dockerfile for the Tenant Management Service (Control Plane)

### Helm Charts
- `3p-charts/`: Third-party Helm charts
    - `istio-baseline/`: istio baseline configuration charts
    - `mysql/`: MySQL database configuration charts
    - `metallb/`: MetalLb configuration scripts
    - `opentelemetry`: open telemetry configuration charts including jaeger and colector services
- `custom-charts/`: Custom application Helm charts regarding the baseline configuration of the environment (non-per tenant provisioning items)
    - `billing/`: Helm chart for the billing service
    - `ingress/`: Helm chart for the kong ingress
    - `kong/`: Helm chart for the king api gateway
    - `notifications/`: Helm chart for the notification service application
    - `omboarding/`: Helm chart for the onboarding service
    - `selfprovisionui/`: Helm chart for the self provision ui
    - `stripe/`: Helm chart which runs the stripe cli (because stripe is integrated in test mode as we aren't a real company)
    - `tenantmanagement`: Helm chart for the tenant management service

### Umbrella Helm chart for per-tenant provisioning
The umbrella chart including helm sub-charts for:
- `hostedsite/`: Helm chart for the hosted site application
- `manager/`: Helm chart for the manager application
- `network-authorization-policies`: Helm chart including per-tenant network and authorization (istio service mesh) authorization policies.

### Observability 
- `grafana/` - Includes configurations for grafana dashboards
- `kiali/` - Inclueds .yaml file for kiali

### Argo Workflows and Argo CD config for the Tenant CD pipeline
- `argowf/` - Includes the argo workflow configuration file (.yaml) for creating per-tenant credentials (both locally and on azure cloud) and save them in the cluster
- `argocd/` - Includes the definition of the argo project, of the tenant application set and of the argo notifications (to get notified of successful provisioning)

### Secrets Folder 
Includes bash scripts to create secrets necessary for the microservices deployment. It includes .example.env.service files that should be copied into .env.service with valid credentials for correct provisioning of the environment.

### Credentials Folder (not inside /infrastructure foler)
Includes credential files related to the argo workflows including: azure_username.txt, azure_password.txt (for azure blob storage) and id_ed25519 containing the private key for the github repo containing the tenant's configuration.


## Building & Pushing Docker Images

**Recommendation**: Use the make file to create the registry and the cluster and build and push the images. The Makefile is present on the root folder [here](../Makefile).

**MUST**: From the root directory of this repo


### Build & Push Hosted Site
```bash
docker build -f infrastructure/Dockerfile.hostedsite -t k3d-registry:5000/contentrus-hostedsite .
```
```bash
docker push k3d-registry:5000/contentrus-hostedsite
```

### Build & Push Hosted Manager
```bash
docker build -f infrastructure/Dockerfile.manager -t k3d-registry:5000/contentrus-manager .
```
```bash
docker push k3d-registry:5000/contentrus-manager
```

### Build & Push Tenant Management
```bash
docker build -f infrastructure/Dockerfile.tenantmanagement -t k3d-registry:5000/contentrus-tenantmanagement .
```
```bash
docker push k3d-registry:5000/contentrus-tenantmanagement
```

### Build & Push Billing Service
```bash
docker build -f infrastructure/Dockerfile.billing -t k3d-registry:5000/contentrus-billing .
```
```bash
docker push k3d-registry:5000/contentrus-billing
```

### Build & Push Self Provision UI
```bash
docker build -f infrastructure/Dockerfile.selfprovision -t k3d-registry:5000/contentrus-selfprovision .
```
```bash
docker push k3d-registry:5000/contentrus-selfprovision
```

### Build and Push Notifications Service
```bash
docker build -f infrastructure/Dockerfile.notificationservice -t k3d-registry:5000/contentrus-notificationservice .
```
```bash
docker push k3d-registry:5000/contentrus-notificationservice
```

### Build and Push Onboarding Service
```bash
docker build -f infrastructure/Dockerfile.onboarding -t k3d-registry:5000/contentrus-onboarding .
```
```bash
docker push k3d-registry:5000/contentrus-onboarding
```