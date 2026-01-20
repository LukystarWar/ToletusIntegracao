# Guia de Instalação - Produção
## Sistema de Controle de Acesso - Academia

Este guia contém todas as instruções para instalar o sistema no computador da academia.

---

## 📋 PRÉ-REQUISITOS

### Software Necessário:
1. **.NET Runtime 10** (ou superior)
   - Download: https://dotnet.microsoft.com/download/dotnet/10.0
   - Instalar apenas o **Runtime** (não precisa do SDK completo)

2. **MySQL Server** (ou XAMPP com MySQL)
   - Se usar XAMPP: https://www.apachefriends.org/
   - Ou MySQL standalone: https://dev.mysql.com/downloads/mysql/

### Equipamentos:
- **Catraca LiteNet2** conectada na rede
- **iDFace Control iD** conectado na rede
- Computador com Windows (mínimo Windows 10)

---

## 🔧 PASSO 1: CONFIGURAR REDE

### 1.1 Definir IPs Estáticos

Configure IP estático para cada dispositivo na rede:

**Exemplo de configuração:**
- Computador servidor: `192.168.1.100`
- Catraca LiteNet2: `192.168.1.200`
- iDFace: `192.168.1.201`

### 1.2 Testar Conectividade

Abra o Prompt de Comando (CMD) e teste:

```bash
ping 192.168.1.200
ping 192.168.1.201
```

Ambos devem responder com sucesso.

---

## 💾 PASSO 2: INSTALAR BANCO DE DADOS

### 2.1 Instalar MySQL/XAMPP

Se usar XAMPP:
1. Instale o XAMPP
2. Inicie o MySQL pelo painel do XAMPP
3. Acesse phpMyAdmin: http://localhost/phpmyadmin

### 2.2 Criar Database

Execute o script `database/schema.sql`:

```bash
cd C:\ToletusIntegracao
mysql -u root -p < database\schema.sql
```

Ou via phpMyAdmin:
1. Crie um novo database: `academia_acesso`
2. Importe o arquivo `schema.sql`

### 2.3 Verificar Tabelas Criadas

Verifique se as tabelas foram criadas:
- `alunos`
- `mensalidades`
- `logs_acesso`

---

## 📦 PASSO 3: INSTALAR O SERVIDOR DE INTEGRAÇÃO

### 3.1 Copiar Arquivos

Copie toda a pasta do projeto para:
```
C:\ToletusIntegracao
```

### 3.2 Configurar appsettings.Production.json

Edite o arquivo:
```
C:\ToletusIntegracao\src\Toletus.IntegracaoServer\appsettings.Production.json
```

Ajuste os seguintes valores:

```json
{
  "Catraca": {
    "IP": "192.168.1.200"  // IP da sua catraca
  },
  "ConnectionStrings": {
    "MySQL": "Server=localhost;Port=3306;Database=academia_acesso;Uid=root;Pwd=SENHA_AQUI;"
  }
}
```

### 3.3 Instalar como Serviço do Windows

Execute como **Administrador**:
```
C:\ToletusIntegracao\install-service.bat
```

Isso irá:
- Compilar a aplicação
- Instalar como serviço do Windows
- Configurar para iniciar automaticamente
- Iniciar o serviço

### 3.4 Verificar Serviço

Abra "Serviços" do Windows (services.msc) e procure:
- **Toletus Integration Server**
- Status deve estar: **Em execução**
- Tipo de inicialização: **Automático**

---

## 👥 PASSO 4: IMPORTAR ALUNOS

### 4.1 Preparar CSV de Alunos

Crie um arquivo CSV com os alunos no formato:

```csv
idface_user_id,nome,email,telefone
1,Maria Silva,maria@email.com,(11) 91111-1111
2,João Santos,joao@email.com,(11) 92222-2222
3,Ana Costa,ana@email.com,(11) 93333-3333
```

**IMPORTANTE:** O `idface_user_id` será o mesmo ID usado no iDFace!

### 4.2 Importar no MySQL

Opção 1 - Via phpMyAdmin:
1. Acesse a tabela `alunos`
2. Clique em "Importar"
3. Selecione seu arquivo CSV
4. Configure: delimitador = vírgula, primeira linha = cabeçalhos

Opção 2 - Via SQL:
```sql
LOAD DATA LOCAL INFILE 'C:/alunos.csv'
INTO TABLE alunos
FIELDS TERMINATED BY ','
ENCLOSED BY '"'
LINES TERMINATED BY '\n'
IGNORE 1 ROWS
(idface_user_id, nome, email, telefone);
```

### 4.3 Criar Mensalidades

Após importar alunos, execute o script para criar mensalidades:
```
database/import_students.sql
```

---

## 📸 PASSO 5: CADASTRAR FACES NO iDFACE

### 5.1 Acessar Interface Web do iDFace

Acesse via navegador: `http://192.168.1.201`

Login padrão:
- Usuário: `admin`
- Senha: `admin`

### 5.2 Configurar iDFace

**Configurações > Rede > Servidor:**
- Tipo: **Standalone Online** ou **iDBlock Next - Primário Online**
- IP do Servidor: `192.168.1.100` (IP do computador)
- Porta: `5000`

**IMPORTANTE:** Use o **MESMO ID** que está no banco de dados!

### 5.3 Cadastrar Fotos

Para cada aluno:
1. Acesse "Usuários" no iDFace
2. Clique em "Adicionar Usuário"
3. **ID**: Use o mesmo número do campo `idface_user_id` no banco
4. Nome: Nome do aluno
5. Tire foto do rosto
6. Salvar

**Processo para funcionária:**
1. Você fornece lista de alunos com seus IDs
2. Funcionária cadastra fotos usando os IDs fornecidos
3. Sistema automaticamente associa face → aluno → mensalidade

---

## ✅ PASSO 6: TESTAR O SISTEMA

### 6.1 Teste Completo

1. **Aluno com mensalidade paga:**
   - Aproxime do iDFace
   - Deve reconhecer o rosto
   - Catraca deve liberar (LED verde)
   - Aluno pode passar

2. **Aluno com mensalidade vencida:**
   - Aproxime do iDFace
   - Reconhece o rosto mas nega acesso
   - Catraca NÃO libera (LED vermelho)
   - Mensagem de mensalidade vencida

### 6.2 Verificar Logs

Logs de acesso são salvos em: `logs_acesso`

Para consultar:
```sql
SELECT * FROM logs_acesso ORDER BY timestamp DESC LIMIT 50;
```

---

## 💰 GERENCIAR MENSALIDADES

### Marcar Mensalidade como Paga

```sql
UPDATE mensalidades
SET status = 'pago', data_pagamento = NOW()
WHERE aluno_id = 1 AND mes_referencia = '2026-01-01';
```

### Marcar como Vencida

```sql
UPDATE mensalidades
SET status = 'vencido'
WHERE aluno_id = 1 AND mes_referencia = '2026-01-01';
```

### Criar Mensalidades do Mês

```sql
INSERT INTO mensalidades (aluno_id, mes_referencia, valor, data_vencimento, status)
SELECT
    id,
    '2026-02-01',
    150.00,
    '2026-02-10',
    'pendente'
FROM alunos
WHERE ativo = 1;
```

---

## 🔧 MANUTENÇÃO

### Reiniciar Serviço

Via CMD como Administrador:
```bash
sc stop ToletusIntegracaoServer
sc start ToletusIntegracaoServer
```

Ou via interface:
1. Abra "Serviços" (services.msc)
2. Localize "Toletus Integration Server"
3. Clique direito > Reiniciar

### Ver Logs do Servidor

Logs ficam em:
```
C:\ToletusIntegracao\logs\
```

### Backup do Banco de Dados

Execute periodicamente:
```bash
mysqldump -u root -p academia_acesso > backup_academia_%date%.sql
```

---

## 🚨 SOLUÇÃO DE PROBLEMAS

### Catraca não libera

1. Verificar se serviço está rodando (services.msc)
2. Verificar IP da catraca no appsettings.json
3. Testar ping para catraca: `ping 192.168.1.200`

### iDFace não reconhece

1. Verificar se iDFace está configurado com IP correto do servidor
2. Testar acesso: `http://192.168.1.100:5000/session_is_valid.fcgi`
3. Verificar se faces estão cadastradas com IDs corretos

### Mensalidade paga mas nega acesso

1. Verificar status no banco:
```sql
SELECT a.nome, m.*
FROM alunos a
JOIN mensalidades m ON a.id = m.aluno_id
WHERE a.idface_user_id = 1;
```

2. Verificar se mes_referencia está correto (mês atual)

### Aluno não cadastrado

1. Verificar se existe no banco:
```sql
SELECT * FROM alunos WHERE idface_user_id = 1;
```

2. Se não existir, cadastrar manualmente

---

## 📞 SUPORTE

Para problemas técnicos:
- Verificar logs em: `C:\ToletusIntegracao\logs\`
- Verificar tabela `logs_acesso` no banco
- Reiniciar serviço do Windows

---

## 📝 RESUMO - WORKFLOW DIÁRIO

1. **Funcionária cadastra novo aluno:**
   - Você adiciona aluno no banco com ID único
   - Funcionária tira foto no iDFace usando esse ID
   - Sistema já funciona automaticamente

2. **Aluno paga mensalidade:**
   - Atualize status para "pago" no banco
   - Imediato: aluno pode acessar

3. **Mensalidade vence:**
   - Após data_vencimento, status automaticamente bloqueia
   - Aluno não consegue mais acessar

4. **Monitorar acessos:**
   - Consulte `logs_acesso` para ver histórico
   - Relatórios de quem acessou e quando

---

**Sistema pronto para produção! 🎉**
