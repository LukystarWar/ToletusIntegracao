-- CSV Import Script for Bulk Student Registration
-- This script helps import students from the gym's existing database
--
-- INSTRUCTIONS:
-- 1. Export students from your gym system to CSV format
-- 2. CSV should have columns: idface_user_id, nome, email, telefone
-- 3. Use MySQL LOAD DATA or the queries below

USE academia_acesso;

-- Example: Manual insert (replace with your student data)
-- INSERT INTO alunos (idface_user_id, nome, email, telefone) VALUES
-- (1, 'Maria Silva', 'maria@email.com', '(11) 91111-1111'),
-- (2, 'João Santos', 'joao@email.com', '(11) 92222-2222'),
-- (3, 'Ana Costa', 'ana@email.com', '(11) 93333-3333');

-- Or use LOAD DATA INFILE (adjust path):
-- LOAD DATA LOCAL INFILE 'C:/alunos.csv'
-- INTO TABLE alunos
-- FIELDS TERMINATED BY ','
-- ENCLOSED BY '"'
-- LINES TERMINATED BY '\n'
-- IGNORE 1 ROWS
-- (idface_user_id, nome, email, telefone);

-- After importing, create mensalidades for all active students
-- This creates January 2026 tuition for all students (adjust as needed)
INSERT INTO mensalidades (aluno_id, mes_referencia, valor, data_vencimento, status)
SELECT
    id,
    '2026-01-01' as mes_referencia,
    150.00 as valor,
    '2026-01-10' as data_vencimento,
    'pendente' as status
FROM alunos
WHERE id NOT IN (SELECT aluno_id FROM mensalidades WHERE mes_referencia = '2026-01-01');

-- Verify import
SELECT COUNT(*) as total_alunos FROM alunos;
SELECT COUNT(*) as total_mensalidades FROM mensalidades;
