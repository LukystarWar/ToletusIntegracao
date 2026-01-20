-- Add test student Ana (ID 2 in iDFace) to database
USE academia_acesso;

-- Insert Ana with iDFace user_id = 2
INSERT INTO alunos (idface_user_id, nome, email, telefone) VALUES
(2, 'Ana', 'ana@example.com', '(11) 98888-8888');

-- Add PAID tuition for Ana (January 2026)
INSERT INTO mensalidades (aluno_id, mes_referencia, valor, data_vencimento, data_pagamento, status) VALUES
((SELECT id FROM alunos WHERE idface_user_id = 2), '2026-01-01', 150.00, '2026-01-10', NOW(), 'pago');

-- Verify data
SELECT a.*, m.*
FROM alunos a
LEFT JOIN mensalidades m ON a.id = m.aluno_id
WHERE a.idface_user_id IN (1, 2);
