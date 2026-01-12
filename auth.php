<?php
require_once 'config.php';

$method = $_SERVER['REQUEST_METHOD'];
$action = $_GET['action'] ?? '';

try {
    $pdo = getDBConnection();
    
    switch ($action) {
        case 'login':
            if ($method === 'POST') {
                $data = json_decode(file_get_contents('php://input'), true);
                $email = $data['email'] ?? '';
                $jelszo = $data['jelszo'] ?? '';
                
                $stmt = $pdo->prepare("SELECT * FROM users WHERE email = ?");
                $stmt->execute([$email]);
                $user = $stmt->fetch();
                
                if ($user && $user['jelszo'] === $jelszo) {
                    // Update last login time
                    $updateStmt = $pdo->prepare("UPDATE users SET utolso_belepes = NOW() WHERE id = ?");
                    $updateStmt->execute([$user['id']]);
                    
                    // Generate session token
                    $token = bin2hex(random_bytes(32));
                    
                    unset($user['jelszo']);
                    echo json_encode([
                        'success' => true,
                        'user' => $user,
                        'token' => $token
                    ]);
                } else {
                    http_response_code(401);
                    echo json_encode(['success' => false, 'error' => 'Hibas email cim vagy jelszo']);
                }
            }
            break;
            
        case 'register':
            if ($method === 'POST') {
                $data = json_decode(file_get_contents('php://input'), true);
                
                // Check if email already exists
                $checkStmt = $pdo->prepare("SELECT id FROM users WHERE email = ?");
                $checkStmt->execute([$data['email']]);
                
                if ($checkStmt->fetch()) {
                    http_response_code(400);
                    echo json_encode(['success' => false, 'error' => 'Ez az email cim mar foglalt']);
                    exit;
                }
                
                // Check if username exists
                $felhasznalonev = explode('@', $data['email'])[0] . rand(100, 999);
                
                $stmt = $pdo->prepare("INSERT INTO users (felhasznalonev, email, jelszo, vezeteknev, keresztnev, telefon, szerepkor) VALUES (?, ?, ?, ?, ?, ?, 'user')");
                $result = $stmt->execute([
                    $felhasznalonev,
                    $data['email'],
                    $data['jelszo'], // Plain text jelszó
                    $data['vezeteknev'] ?? '',
                    $data['keresztnev'] ?? '',
                    $data['telefon'] ?? ''
                ]);
                
                if ($result) {
                    echo json_encode(['success' => true, 'message' => 'Sikeres regisztracio! Most mar bejelentkezhet.']);
                } else {
                    echo json_encode(['success' => false, 'error' => 'Hiba tortent a regisztracio soran']);
                }
            }
            break;
            
        case 'logout':
            echo json_encode(['success' => true]);
            break;
            
        case 'verify':
            if ($method === 'POST') {
                $data = json_decode(file_get_contents('php://input'), true);
                $token = $data['token'] ?? '';
                
                // Simple token check from localStorage
                if ($token) {
                    echo json_encode(['success' => true]);
                } else {
                    http_response_code(401);
                    echo json_encode(['success' => false, 'error' => 'Ervenytelen token']);
                }
            }
            break;
            
        default:
            http_response_code(400);
            echo json_encode(['success' => false, 'error' => 'Ismeretlen muvelet: ' . $action]);
    }
} catch (PDOException $e) {
    http_response_code(500);
    echo json_encode(['success' => false, 'error' => 'Adatbazis hiba: ' . $e->getMessage()]);
} catch (Exception $e) {
    http_response_code(500);
    echo json_encode(['success' => false, 'error' => 'Szerver hiba: ' . $e->getMessage()]);
}
?>
