# MensajeriaIA

## Guía rápida de arranque

### 📦 Paso 1: Levantar la Infraestructura de Base de Datos (Docker)

```bash
docker-compose up -d
```

### 🔑 Paso 2: Levantar el Servicio de Autenticación (AuthService - Puerto 5001)

```bash
dotnet run --project AuthService/AuthService.csproj --urls "http://localhost:5001"
```

### 💳 Paso 3: Levantar el Servicio de Facturación (BillingService - Puerto 5002)

```bash
dotnet run --project BillingService/BillingService.csproj --urls "http://localhost:5002"
```

### 💬 Paso 4: Levantar el Servicio de Chat y WebSockets (ChatService - Puerto 5003)

```bash
dotnet run --project ChatService/ChatService.csproj --urls "http://localhost:5003"
```

### 💻 Paso 5: Levantar la Interfaz de Usuario (Frontend - React/Vite)

```bash
cd frontend
npm run dev
```

> Opcional: la primera vez que abras el proyecto en esta máquina, instala dependencias con:
>
> ```bash
> npm install
> ```
