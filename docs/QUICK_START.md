# ⚡ QUICK START - Instalação Rápida

## Para quem tem pressa! 🚀

### Passo 1: Pré-requisitos (5 min)
```bash
# Instalar .NET Runtime 10
https://dotnet.microsoft.com/download/dotnet/10.0

# Instalar XAMPP (MySQL)
https://www.apachefriends.org/
```

### Passo 2: IPs Estáticos (2 min)
- Servidor: `192.168.1.100`
- Catraca: `192.168.1.200`
- iDFace: `192.168.1.201`

### Passo 3: Banco de Dados (2 min)
```bash
cd C:\ToletusIntegracao
mysql -u root -p < database\schema.sql
```

### Passo 4: Configurar (1 min)
Editar: `src\Toletus.IntegracaoServer\appsettings.Production.json`
```json
{
  "Catraca": { "IP": "192.168.1.200" },
  "ConnectionStrings": { "MySQL": "...sua_senha..." }
}
```

### Passo 5: Instalar Serviço (2 min)
```bash
# Como Administrador:
install-service.bat
```

### Passo 6: Importar Alunos (5 min)
```sql
-- Ajustar e executar:
database/import_students.sql
```

### Passo 7: Configurar iDFace (3 min)
- Acessar: `http://192.168.1.201`
- Login: admin/admin
- Servidor: `192.168.1.100:5000`

### Passo 8: Cadastrar Fotos
- Dar lista de IDs para funcionária
- Seguir: `MANUAL_FUNCIONARIA.md`

### Passo 9: Testar! ✅
- Aluno pago → deve passar
- Aluno vencido → deve bloquear

---

## Deu Problema? 🔧

```bash
# Ver se serviço está rodando
sc query ToletusIntegracaoServer

# Reiniciar
sc stop ToletusIntegracaoServer
sc start ToletusIntegracaoServer

# Ver logs
type C:\ToletusIntegracao\logs\*.log
```

---

## Dúvidas? 📚
Veja: `GUIA_INSTALACAO_PRODUCAO.md` (completo e detalhado)

**Tempo total: ~20 minutos!**
