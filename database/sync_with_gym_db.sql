-- Script to Sync with Gym's Existing Database
-- This helps match students from your existing gym system to the access control system

USE academia_acesso;

-- ===========================================================
-- OPTION 1: If you have a separate gym management database
-- ===========================================================

-- Example: Sync from external database "academia_sistema"
-- Adjust table and column names to match YOUR system

/*
INSERT INTO academia_acesso.alunos (idface_user_id, nome, email, telefone)
SELECT
    id as idface_user_id,          -- Use the same ID from your system
    nome_completo as nome,
    email_contato as email,
    telefone_celular as telefone
FROM academia_sistema.cadastro_alunos
WHERE ativo = 1                     -- Only active students
AND id NOT IN (SELECT idface_user_id FROM academia_acesso.alunos);  -- Avoid duplicates

-- Then create mensalidades based on payment status in your system
INSERT INTO academia_acesso.mensalidades (aluno_id, mes_referencia, valor, data_vencimento, data_pagamento, status)
SELECT
    aa.id as aluno_id,
    DATE_FORMAT(CURDATE(), '%Y-%m-01') as mes_referencia,
    p.valor_mensalidade as valor,
    p.data_vencimento,
    p.data_pagamento,
    CASE
        WHEN p.data_pagamento IS NOT NULL THEN 'pago'
        WHEN p.data_vencimento < CURDATE() THEN 'vencido'
        ELSE 'pendente'
    END as status
FROM academia_sistema.pagamentos p
JOIN academia_acesso.alunos aa ON p.aluno_id = aa.idface_user_id
WHERE p.mes_referencia = DATE_FORMAT(CURDATE(), '%Y-%m-01')
AND aa.id NOT IN (
    SELECT aluno_id FROM academia_acesso.mensalidades
    WHERE mes_referencia = DATE_FORMAT(CURDATE(), '%Y-%m-01')
);
*/

-- ===========================================================
-- OPTION 2: Manual mapping for existing students
-- ===========================================================

-- If students already exist in your system but don't have idface_user_id yet:
-- Create a temporary mapping table

CREATE TEMPORARY TABLE temp_mapping (
    gym_system_id INT,
    student_name VARCHAR(255),
    idface_id INT
);

-- Insert mappings (you'll fill this based on your gym system)
-- Example:
INSERT INTO temp_mapping VALUES
(100, 'Maria Silva', 1),
(101, 'João Santos', 2),
(102, 'Ana Costa', 3);

-- Now import to access control system
INSERT INTO academia_acesso.alunos (idface_user_id, nome, email, telefone)
SELECT
    tm.idface_id as idface_user_id,
    gs.nome,
    gs.email,
    gs.telefone
FROM temp_mapping tm
JOIN academia_sistema.cadastro_alunos gs ON tm.gym_system_id = gs.id
WHERE tm.idface_id NOT IN (SELECT idface_user_id FROM academia_acesso.alunos);

-- ===========================================================
-- OPTION 3: Export students for employee to register photos
-- ===========================================================

-- Generate a list for the employee with ID numbers for photo registration
SELECT
    idface_user_id as 'ID para Cadastro',
    nome as 'Nome do Aluno',
    telefone as 'Telefone',
    'PENDENTE' as 'Status Foto'
FROM academia_acesso.alunos
WHERE idface_user_id NOT IN (
    -- Assuming you track which ones have photos registered
    SELECT DISTINCT idface_user_id FROM logs_acesso
)
ORDER BY idface_user_id;

-- Save this to CSV and give to employee!

-- ===========================================================
-- OPTION 4: Check sync status
-- ===========================================================

-- Students in access system but no mensalidade:
SELECT a.*
FROM alunos a
LEFT JOIN mensalidades m ON a.id = m.aluno_id
    AND m.mes_referencia = DATE_FORMAT(CURDATE(), '%Y-%m-01')
WHERE m.id IS NULL;

-- Students with payment but no access yet today:
SELECT
    a.nome,
    m.status,
    COUNT(l.id) as acessos_hoje
FROM alunos a
JOIN mensalidades m ON a.id = m.aluno_id
    AND m.mes_referencia = DATE_FORMAT(CURDATE(), '%Y-%m-01')
LEFT JOIN logs_acesso l ON a.id = l.aluno_id
    AND DATE(l.timestamp) = CURDATE()
WHERE m.status = 'pago'
GROUP BY a.id, a.nome, m.status
ORDER BY acessos_hoje ASC;

-- ===========================================================
-- TIPS FOR PRODUCTION:
-- ===========================================================

-- 1. Keep IDs synchronized:
--    - Use the SAME ID from your gym system as idface_user_id
--    - This makes matching automatic

-- 2. Regular sync:
--    - Run sync script daily or when new students enroll
--    - Update mensalidades based on payment system

-- 3. Workflow:
--    - New student enrolls in gym system → gets ID 123
--    - Run sync script → student appears in academia_acesso with idface_user_id=123
--    - Employee registers photo in iDFace using ID 123
--    - Everything automatically connected!

-- ===========================================================
