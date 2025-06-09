# Content R Us - Multi-Tenancy CMS System based on open source PiranhaCMS

A multi-tenant CMS platform developed within the scope of Software Architectures subject.

### Repository Overview for ContentRUs

- [``ContentRus.ApplicationPlane/``](ContentRus.ApplicationPlane/) - contains the developed code for the Application Plane microservices
- [``ContentRus.ControlPlane/``](ContentRus.ControlPlane/) - contains the developed Control Plane microservices
- [``credentials/``](credentials/) - placeholder folder to add some credentials as specified in [README.md](credentials/README.md)
- [``infrastructure/``](infrastructure/) - contains all of the infrastructure provisioning scripts, mainly using helm charts and .yaml files
- [``reports_diagrams/``](reports_diagrams/) - contains the reports, diagrams and presentations made throughout the assignment.
- [``Makefile``](Makefile) - the Makefile contains all that is necessary to create k3d registry, cluster, build and push the docker images and provision the baseline infrastructure of the application.

As this is a fork from PiranhaCMS's [piranha.core](https://github.com/PiranhaCMS/piranha.core) it includes application code regarding piranha included in [``core/``](core/), [``data/``](data/), [``identity/``](identity/) and [``test/``](test/)

### ArgoCD Github Sources

ArgoCD has configured as git sources (gitops):
- The current repo for base infrastructure: [contentrus.multiTenancy](https://github.com/Migas77/contentrus.multiTenancy)
- Another repo for per-tenant configuration files: [contentrus.multiTenancy.argocdHelmCharts](https://github.com/Migas77/contentrus.multiTenancy.argocdHelmCharts)