<?php
header('Content-Type: application/json; charset=utf-8');
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: GET, POST, DELETE, OPTIONS');
header('Access-Control-Allow-Headers: Content-Type');

if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
    exit(0);
}

require_once 'config.php';

$pdo = getDBConnection();
$action = $_GET['action'] ?? '';

try {
    switch ($action) {
        case 'get':
            getCart($pdo);
            break;
        case 'add':
            addToCart($pdo);
            break;
        case 'update':
            updateCart($pdo);
            break;
        case 'remove':
            removeFromCart($pdo);
            break;
        case 'clear':
            clearCart($pdo);
            break;
        default:
            echo json_encode(['success' => false, 'error' => 'Ismeretlen művelet']);
    }
} catch (Exception $e) {
    echo json_encode(['success' => false, 'error' => $e->getMessage()]);
}

function getUserId() {
    $userId = $_GET['user_id'] ?? $_POST['user_id'] ?? null;
    if (!$userId) {
        $data = json_decode(file_get_contents('php://input'), true);
        $userId = $data['user_id'] ?? null;
    }
    return $userId ? intval($userId) : null;
}

function getCart($pdo) {
    $userId = getUserId();
    
    if (!$userId) {
        echo json_encode(['success' => true, 'items' => [], 'total' => 0, 'logged_in' => false]);
        return;
    }
    
    $stmt = $pdo->prepare("
        SELECT k.id, k.mennyiseg, k.alkatresz_id, k.olaj_id,
               COALESCE(a.nev, o.nev) as nev,
               COALESCE(a.cikkszam, o.cikkszam) as cikkszam,
               COALESCE(COALESCE(a.akcios_ar, a.ar), COALESCE(o.akcios_ar, o.ar)) as ar,
               COALESCE(a.gyarto, o.gyarto) as gyarto
        FROM kosar k
        LEFT JOIN alkatreszek a ON k.alkatresz_id = a.id
        LEFT JOIN olajok o ON k.olaj_id = o.id
        WHERE k.user_id = ?
    ");
    $stmt->execute([$userId]);
    $items = $stmt->fetchAll(PDO::FETCH_ASSOC);
    
    $total = 0;
    foreach ($items as &$item) {
        $item['osszeg'] = $item['ar'] * $item['mennyiseg'];
        $total += $item['osszeg'];
    }
    
    echo json_encode(['success' => true, 'items' => $items, 'total' => $total, 'logged_in' => true]);
}

function addToCart($pdo) {
    $data = json_decode(file_get_contents('php://input'), true);
    
    $userId = $data['user_id'] ?? null;
    $alkatreszId = $data['alkatresz_id'] ?? null;
    $olajId = $data['olaj_id'] ?? null;
    $mennyiseg = $data['mennyiseg'] ?? 1;
    
    if (!$userId) {
        echo json_encode(['success' => false, 'error' => 'A kosárba rakáshoz be kell jelentkezni!', 'require_login' => true]);
        return;
    }
    
    if (empty($alkatreszId) && empty($olajId)) {
        echo json_encode(['success' => false, 'error' => 'Hiányzó termék azonosító']);
        return;
    }
    
    // Ellenőrizzük, hogy már a kosárban van-e
    if ($alkatreszId) {
        $stmt = $pdo->prepare("SELECT id, mennyiseg FROM kosar WHERE user_id = ? AND alkatresz_id = ?");
        $stmt->execute([$userId, $alkatreszId]);
    } else {
        $stmt = $pdo->prepare("SELECT id, mennyiseg FROM kosar WHERE user_id = ? AND olaj_id = ?");
        $stmt->execute([$userId, $olajId]);
    }
    $existing = $stmt->fetch(PDO::FETCH_ASSOC);
    
    if ($existing) {
        // Mennyiség növelése
        $stmt = $pdo->prepare("UPDATE kosar SET mennyiseg = mennyiseg + ? WHERE id = ?");
        $stmt->execute([$mennyiseg, $existing['id']]);
    } else {
        // Új tétel hozzáadása
        $stmt = $pdo->prepare("INSERT INTO kosar (user_id, alkatresz_id, olaj_id, mennyiseg) VALUES (?, ?, ?, ?)");
        $stmt->execute([$userId, $alkatreszId ?: null, $olajId ?: null, $mennyiseg]);
    }
    
    echo json_encode(['success' => true, 'message' => 'Termék hozzáadva a kosárhoz']);
}

function updateCart($pdo) {
    $data = json_decode(file_get_contents('php://input'), true);
    
    $cartId = $data['cart_id'] ?? '';
    $mennyiseg = $data['mennyiseg'] ?? 1;
    $userId = $data['user_id'] ?? null;
    
    if (!$userId) {
        echo json_encode(['success' => false, 'error' => 'Nincs bejelentkezve']);
        return;
    }
    
    if (empty($cartId)) {
        echo json_encode(['success' => false, 'error' => 'Hiányzó kosár ID']);
        return;
    }
    
    if ($mennyiseg <= 0) {
        // Törlés ha 0 vagy negatív
        $stmt = $pdo->prepare("DELETE FROM kosar WHERE id = ? AND user_id = ?");
        $stmt->execute([$cartId, $userId]);
    } else {
        $stmt = $pdo->prepare("UPDATE kosar SET mennyiseg = ? WHERE id = ? AND user_id = ?");
        $stmt->execute([$mennyiseg, $cartId, $userId]);
    }
    
    echo json_encode(['success' => true]);
}

function removeFromCart($pdo) {
    $data = json_decode(file_get_contents('php://input'), true);
    
    $cartId = $data['cart_id'] ?? '';
    $userId = $data['user_id'] ?? null;
    
    if (!$userId) {
        echo json_encode(['success' => false, 'error' => 'Nincs bejelentkezve']);
        return;
    }
    
    if (empty($cartId)) {
        echo json_encode(['success' => false, 'error' => 'Hiányzó kosár ID']);
        return;
    }
    
    $stmt = $pdo->prepare("DELETE FROM kosar WHERE id = ? AND user_id = ?");
    $stmt->execute([$cartId, $userId]);
    
    echo json_encode(['success' => true]);
}

function clearCart($pdo) {
    $userId = getUserId();
    
    if (!$userId) {
        echo json_encode(['success' => false, 'error' => 'Nincs bejelentkezve']);
        return;
    }
    
    $stmt = $pdo->prepare("DELETE FROM kosar WHERE user_id = ?");
    $stmt->execute([$userId]);
    
    echo json_encode(['success' => true]);
}
?>
