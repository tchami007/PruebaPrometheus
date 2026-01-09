# 🚀 Guía: Subir Aplicación de Prometheus a GitHub

## 📋 Requisitos Previos

- Git instalado en tu sistema
- Cuenta de GitHub creada
- Proyecto funcionando en tu máquina local

## 🔍 Verificar Instalación de Git

```cmd
# Verificar que Git esté instalado
git --version

# Si no está instalado, descargar desde: https://git-scm.com/
```

## 📁 Preparar el Proyecto

### Paso 1: Crear .gitignore

Crear archivo `.gitignore` en la raíz del proyecto:

```gitignore
# Build results
[Dd]ebug/
[Dd]ebugPublic/
[Rr]elease/
[Rr]eleases/
x64/
x86/
[Ww][Ii][Nn]32/
[Aa][Rr][Mm]/
[Aa][Rr][Mm]64/
bld/
[Bb]in/
[Oo]bj/
[Ll]og/
[Ll]ogs/

# Visual Studio 2015/2017 cache/options directory
.vs/

# User-specific files
*.rsuser
*.suo
*.user
*.userosscache
*.sln.docstates

# Docker volumes data (no subir datos de contenedores)
prometheus_data/
grafana_data/

# OS generated files
.DS_Store
.DS_Store?
._*
.Spotlight-V100
.Trashes
ehthumbs.db
Thumbs.db

# IDE files
.vscode/
.idea/

# Logs
*.log
npm-debug.log*
yarn-debug.log*
yarn-error.log*

# Environment variables
.env
.env.local
.env.development.local
.env.test.local
.env.production.local
```

### Paso 2: Verificar Estructura del Proyecto

Tu proyecto debe tener esta estructura:

```
PruebaPrometheus/
├── .gitignore                    # ✅ Crear
├── README.md                     # ✅ Ya tienes
├── PruebaPrometheus.csproj       # ✅ Ya tienes
├── Program.cs                    # ✅ Ya tienes
├── docker-compose.yml            # ✅ Ya tienes
├── test-api.bat                  # ✅ Ya tienes
├── Application/
│   └── Services/
│       └── ExampleService.cs     # ✅ Ya tienes
├── Controllers/
│   └── ExampleController.cs      # ✅ Ya tienes
├── Observability/
│   └── Metrics/
│       └── ExampleMetrics.cs     # ✅ Ya tienes
├── monitoring/
│   ├── prometheus.yml            # ✅ Ya tienes
│   └── grafana/                  # ✅ Ya tienes
└── doc/
    ├── plan-aplicacion-demo-prometheus.md
    ├── ejemplos-requests.md
    ├── tutorial-prometheus-net.md
    ├── 03-instalacion-monitoring.md
    └── 04-subir-a-github.md      # ✅ Este archivo
```

## 🌐 Crear Repositorio en GitHub

### Paso 3: Crear Repositorio Online

1. Ve a https://github.com
2. Click en "New repository" (botón verde)
3. Configurar:
   - **Repository name**: `PruebaPrometheus-NET`
   - **Description**: `Aplicación de demostración de métricas Prometheus con ASP.NET Core`
   - **Visibility**: Public (recomendado para portfolios)
   - **NO** marcar "Add a README file" (ya lo tienes)
   - **NO** agregar .gitignore (ya lo crearás)
   - **License**: MIT License (recomendado)

4. Click "Create repository"

### Paso 4: Configurar Git Local

```cmd
# Ir a la carpeta del proyecto
cd d:\NET\PruebaPrometheus

# Configurar Git (si es primera vez)
git config --global user.name "Tu Nombre"
git config --global user.email "tu-email@gmail.com"

# Inicializar repositorio local
git init

# Verificar que .gitignore esté creado
dir .gitignore
```

### Paso 5: Hacer Primer Commit

```cmd
# Agregar todos los archivos al staging area
git add .

# Verificar qué se va a commitear
git status

# Hacer primer commit
git commit -m "Initial commit: Aplicación demo Prometheus + Grafana con ASP.NET Core

- Configuración completa de métricas Prometheus
- Endpoints de ejemplo para operaciones y transacciones  
- Definición centralizada de métricas (Counter, Histogram)
- Docker Compose para Prometheus + Grafana
- Dashboard pre-configurado de Grafana
- Documentación completa y tutoriales
- Scripts de prueba automatizados"
```

### Paso 6: Conectar con GitHub

```cmd
# Agregar remote origin (cambiar por tu URL de GitHub)
git remote add origin https://github.com/TU-USUARIO/PruebaPrometheus-NET.git

# Verificar conexión
git remote -v

# Subir al repositorio
git push -u origin main
```

## 📝 Actualizar README Principal

### Paso 7: Mejorar el README

Tu `README.md` actual está bien, pero podrías mejorarlo:

```markdown
# 📊 PruebaPrometheus - Aplicación Demo de Métricas

Aplicación ASP.NET Core Web API que demuestra la implementación completa de métricas Prometheus con prometheus-net, incluyendo configuración de Grafana y dashboards.

## 🚀 Características

- ✅ **ASP.NET Core 8** con métricas Prometheus
- ✅ **Métricas personalizadas** (Counter, Histogram, Labels)
- ✅ **Docker Compose** para Prometheus + Grafana
- ✅ **Dashboard pre-configurado** de Grafana
- ✅ **Documentación completa** con tutoriales
- ✅ **Scripts de prueba** automatizados

## 🎯 Quick Start

### 1. Ejecutar la aplicación
```bash
dotnet run
```

### 2. Probar las APIs
```bash
# Windows
test-api.bat

# Ver métricas
curl https://localhost:7001/metrics
```

### 3. Monitoreo con Docker
```bash
docker-compose up -d
```

- **Grafana**: http://localhost:3000 (admin/admin123)
- **Prometheus**: http://localhost:9090

## 📚 Documentación

- [📖 Tutorial Prometheus-Net](doc/tutorial-prometheus-net.md)
- [🔧 Instalación de Monitoring](doc/03-instalacion-monitoring.md)  
- [🧪 Ejemplos de Requests](doc/ejemplos-requests.md)
- [📋 Plan del Proyecto](doc/plan-aplicacion-demo-prometheus.md)

## 🛠️ Tecnologías

- ASP.NET Core 8
- prometheus-net.AspNetCore  
- Docker & Docker Compose
- Prometheus 2.45.0
- Grafana 10.0.0

## 📊 Métricas Implementadas

- `example_requests_total` - Total de requests
- `example_operations_total{operation_type}` - Operaciones por tipo
- `example_processing_seconds` - Histograma de latencia
- `example_transactions_by_path_total{path,transaction_type}` - Transacciones segmentadas

## 🎓 Propósito Educativo

Este proyecto está diseñado para aprender:
- Configuración de métricas Prometheus en .NET
- Uso de diferentes tipos de métricas (Counter, Histogram)
- Implementación de labels para segmentación
- Integración con Grafana para visualización
- Buenas prácticas de observabilidad

## 📄 Licencia

MIT License - ver [LICENSE](LICENSE) para detalles.
```

## 🔄 Flujo de Trabajo Futuro

### Para Actualizaciones

```cmd
# Hacer cambios en el código...

# Agregar cambios
git add .

# Commit con mensaje descriptivo
git commit -m "Agregar nueva métrica de base de datos"

# Subir cambios
git push origin main
```

### Crear Releases

```cmd
# Crear tag para versión
git tag -a v1.0.0 -m "Primera versión completa con Prometheus + Grafana"

# Subir tags
git push origin --tags
```

## 🎯 Consideraciones Especiales

### Archivos que NO se suben (ya están en .gitignore):

- `bin/` y `obj/` - Archivos compilados
- `.vs/` - Configuración de Visual Studio
- `prometheus_data/` y `grafana_data/` - Datos de contenedores
- `*.log` - Archivos de logs

### Archivos que SÍ se suben:

- Todo el código fuente (`.cs`, `.csproj`)
- Configuraciones (`.yml`, `.json`)
- Documentación (`.md`)
- Scripts (`.bat`)
- Docker Compose file

## 💡 Tips Adicionales

### Para Portfolio

1. **Agregar screenshots** del dashboard de Grafana
2. **Crear GitHub Actions** para CI/CD
3. **Agregar badges** de build status
4. **Documentar casos de uso** reales

### Para Colaboración

1. **Crear Issues** para mejoras futuras
2. **Usar branches** para features nuevas
3. **Pull Requests** para revisión de código
4. **GitHub Projects** para roadmap

## 🆘 Troubleshooting

### Error: "Repository not found"
```cmd
# Verificar URL del remote
git remote get-url origin

# Cambiar si es necesario
git remote set-url origin https://github.com/TU-USUARIO/PruebaPrometheus-NET.git
```

### Error: "Permission denied"
```cmd
# Configurar autenticación con token de GitHub
# Ir a GitHub Settings > Developer settings > Personal access tokens
# Crear token y usarlo como password
```

### Error: "Large files"
```cmd
# Ver archivos grandes
git ls-files --others --ignored --exclude-standard

# Agregar a .gitignore si es necesario
```

## 📞 Próximos Pasos

Una vez subido a GitHub:

1. ✅ **Verificar** que todo se subió correctamente
2. ✅ **Probar** clonando en otra carpeta
3. ✅ **Agregar** screenshots al README
4. ✅ **Compartir** el link del repositorio
5. ✅ **Considerar** crear GitHub Pages para documentación

---

🎉 **¡Tu aplicación ya está en GitHub!** 

URL del repositorio: `https://github.com/TU-USUARIO/PruebaPrometheus-NET`