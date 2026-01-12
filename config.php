<?php
error_reporting(0);
ini_set('display_errors', 0);
ob_start();

// CORS headers - ezeknek ELŐSZÖR kell jönniük
header('Content-Type: application/json; charset=utf-8');
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS');
header('Access-Control-Allow-Headers: Content-Type, Authorization');

if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
    http_response_code(200);
    exit(0);
}

// Adatbázis konfiguráció - XAMPP localhost
define('DB_HOST', '127.0.0.1');
define('DB_NAME', 'autoalkatresz_db');
define('DB_USER', 'root');
define('DB_PASS', ''); // XAMPP alapértelmezett: üres jelszó

// PDO kapcsolat létrehozása
function getDBConnection() {
    try {
        $dsn = "mysql:host=" . DB_HOST . ";dbname=" . DB_NAME . ";charset=utf8mb4";
        $options = [
            PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
            PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
            PDO::ATTR_EMULATE_PREPARES => false
        ];
        return new PDO($dsn, DB_USER, DB_PASS, $options);
    } catch (PDOException $e) {
        http_response_code(500);
        echo json_encode(['success' => false, 'error' => 'Adatbazis kapcsolodasi hiba: ' . $e->getMessage()]);
        exit;
    }
}
?>
