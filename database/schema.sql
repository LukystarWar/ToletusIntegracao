-- Database for Access Control Integration
-- LiteNet2 + Control iD iDFace

CREATE DATABASE IF NOT EXISTS academia_acesso;
USE academia_acesso;

-- Students table
CREATE TABLE IF NOT EXISTS alunos (
    id INT AUTO_INCREMENT PRIMARY KEY,
    idface_user_id BIGINT NOT NULL UNIQUE COMMENT 'ID from Control iD iDFace device',
    nome VARCHAR(255) NOT NULL,
    email VARCHAR(255),
    telefone VARCHAR(20),
    data_cadastro DATETIME DEFAULT CURRENT_TIMESTAMP,
    ativo BOOLEAN DEFAULT TRUE,
    INDEX idx_idface_user_id (idface_user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Monthly tuition payments
CREATE TABLE IF NOT EXISTS mensalidades (
    id INT AUTO_INCREMENT PRIMARY KEY,
    aluno_id INT NOT NULL,
    mes_referencia DATE NOT NULL COMMENT 'First day of the month (e.g., 2026-01-01)',
    valor DECIMAL(10,2) NOT NULL,
    data_vencimento DATE NOT NULL,
    data_pagamento DATETIME NULL,
    status ENUM('pendente', 'pago', 'vencido') DEFAULT 'pendente',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (aluno_id) REFERENCES alunos(id) ON DELETE CASCADE,
    UNIQUE KEY uk_aluno_mes (aluno_id, mes_referencia),
    INDEX idx_status (status),
    INDEX idx_vencimento (data_vencimento)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Access logs
CREATE TABLE IF NOT EXISTS logs_acesso (
    id INT AUTO_INCREMENT PRIMARY KEY,
    aluno_id INT NULL,
    idface_user_id BIGINT NOT NULL,
    nome_usuario VARCHAR(255),
    autorizado BOOLEAN NOT NULL,
    motivo VARCHAR(500),
    confidence INT COMMENT 'Face recognition confidence from iDFace',
    device_id VARCHAR(50),
    timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (aluno_id) REFERENCES alunos(id) ON DELETE SET NULL,
    INDEX idx_timestamp (timestamp),
    INDEX idx_aluno (aluno_id),
    INDEX idx_autorizado (autorizado)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Insert test data: Lucas with ID 1 (overdue tuition)
INSERT INTO alunos (idface_user_id, nome, email, telefone) VALUES
(1, 'Lucas', 'lucas@example.com', '(11) 99999-9999');

-- Add overdue tuition for Lucas (January 2026 - vencimento: 2026-01-10)
INSERT INTO mensalidades (aluno_id, mes_referencia, valor, data_vencimento, status) VALUES
(1, '2026-01-01', 150.00, '2026-01-10', 'vencido');

-- Query to check access authorization
-- SELECT a.*, m.*
-- FROM alunos a
-- LEFT JOIN mensalidades m ON a.id = m.aluno_id
--   AND m.mes_referencia = DATE_FORMAT(CURDATE(), '%Y-%m-01')
-- WHERE a.idface_user_id = 1;
