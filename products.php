<?php
header('Content-Type: application/json; charset=utf-8');
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: GET, POST, DELETE, OPTIONS');
header('Access-Control-Allow-Headers: Content-Type');

if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
    exit(0);
}

require_once 'config.php';

$method = $_SERVER['REQUEST_METHOD'];
$action = $_GET['action'] ?? 'list';

try {
    $pdo = getDBConnection();

    switch ($action) {
        case 'search_cikkszam':
            $cikkszam = $_GET['cikkszam'] ?? '';
            
            if (empty($cikkszam)) {
                echo json_encode(['success' => false, 'error' => 'Cikkszam megadasa kotelezo']);
                break;
            }
            
            $search = '%' . $cikkszam . '%';
            $stmt = $pdo->prepare("
                SELECT a.*, k.nev as kategoria 
                FROM alkatreszek a 
                LEFT JOIN kategoriak k ON a.kategoria_id = k.id
                WHERE (a.cikkszam LIKE ? OR a.oe_szam LIKE ? OR a.nev LIKE ?) AND a.aktiv = 1
                ORDER BY a.nev
            ");
            $stmt->execute([$search, $search, $search]);
            $products = $stmt->fetchAll(PDO::FETCH_ASSOC);
            
            echo json_encode(['success' => true, 'products' => $products]);
            break;
        
        case 'search':
            $modell_id = $_GET['modell_id'] ?? 0;
            $motor_id = $_GET['motor_id'] ?? null;
            $tipus = $_GET['tipus'] ?? 'szemely';
            
            $sql = "
                SELECT DISTINCT a.*, k.nev as kategoria_nev 
                FROM alkatreszek a 
                LEFT JOIN kategoriak k ON a.kategoria_id = k.id
                INNER JOIN alkatresz_auto aa ON a.id = aa.alkatresz_id
                WHERE aa.modell_id = ? AND a.aktiv = 1
            ";
            $params = [$modell_id];
            
            if ($motor_id) {
                $sql .= " AND (aa.motor_id = ? OR aa.motor_id IS NULL)";
                $params[] = $motor_id;
            }
            
            $sql .= " ORDER BY a.nev";
            
            $stmt = $pdo->prepare($sql);
            $stmt->execute($params);
            $products = $stmt->fetchAll(PDO::FETCH_ASSOC);
            
            echo json_encode(['success' => true, 'products' => $products]);
            break;
            
        case 'list':
            $where = ["a.aktiv = 1"];
            $params = [];
            
            if (!empty($_GET['kategoria'])) {
                $where[] = "a.kategoria_id = ?";
                $params[] = $_GET['kategoria'];
            }
            if (!empty($_GET['gyarto'])) {
                $where[] = "a.gyarto = ?";
                $params[] = $_GET['gyarto'];
            }
            if (!empty($_GET['kereses'])) {
                $where[] = "(a.nev LIKE ? OR a.cikkszam LIKE ? OR a.oe_szam LIKE ?)";
                $search = '%' . $_GET['kereses'] . '%';
                $params[] = $search;
                $params[] = $search;
                $params[] = $search;
            }
            if (!empty($_GET['modell_id'])) {
                $where[] = "aa.modell_id = ?";
                $params[] = $_GET['modell_id'];
            }
            
            $whereClause = implode(' AND ', $where);
            
            $page = max(1, intval($_GET['page'] ?? 1));
            $limit = max(1, min(100, intval($_GET['limit'] ?? 12)));
            $offset = ($page - 1) * $limit;
            
            $sql = "
                SELECT DISTINCT a.*, k.nev as kategoria_nev 
                FROM alkatreszek a 
                LEFT JOIN kategoriak k ON a.kategoria_id = k.id
                LEFT JOIN alkatresz_auto aa ON a.id = aa.alkatresz_id
                WHERE $whereClause
                ORDER BY a.letrehozva DESC
                LIMIT $limit OFFSET $offset
            ";
            
            $stmt = $pdo->prepare($sql);
            $stmt->execute($params);
            $products = $stmt->fetchAll(PDO::FETCH_ASSOC);
            
            echo json_encode(['success' => true, 'products' => $products]);
            break;
            
        case 'get':
            $id = $_GET['id'] ?? 0;
            $stmt = $pdo->prepare("SELECT a.*, k.nev as kategoria_nev FROM alkatreszek a LEFT JOIN kategoriak k ON a.kategoria_id = k.id WHERE a.id = ?");
            $stmt->execute([$id]);
            $product = $stmt->fetch(PDO::FETCH_ASSOC);
            
            if ($product) {
                echo json_encode(['success' => true, 'product' => $product]);
            } else {
                echo json_encode(['success' => false, 'error' => 'Termek nem talalhato']);
            }
            break;
            
        case 'create':
            if ($method === 'POST') {
                $data = json_decode(file_get_contents('php://input'), true);
                
                $stmt = $pdo->prepare("
                    INSERT INTO alkatreszek (cikkszam, nev, leiras, kategoria_id, ar, akcios_ar, keszlet, gyarto, oe_szam, kep_url) 
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                ");
                $stmt->execute([
                    $data['cikkszam'],
                    $data['nev'],
                    $data['leiras'] ?? '',
                    $data['kategoria_id'] ?? null,
                    $data['ar'],
                    $data['akcios_ar'] ?? null,
                    $data['keszlet'] ?? 0,
                    $data['gyarto'] ?? '',
                    $data['oe_szam'] ?? '',
                    $data['kep_url'] ?? ''
                ]);
                
                $productId = $pdo->lastInsertId();
                echo json_encode(['success' => true, 'id' => $productId, 'message' => 'Termek sikeresen letrehozva']);
            }
            break;
            
        case 'update':
            if ($method === 'POST') {
                $data = json_decode(file_get_contents('php://input'), true);
                $id = $data['id'] ?? 0;
                
                $stmt = $pdo->prepare("
                    UPDATE alkatreszek SET 
                        cikkszam = ?, nev = ?, leiras = ?, kategoria_id = ?, 
                        ar = ?, akcios_ar = ?, keszlet = ?, gyarto = ?, oe_szam = ?, kep_url = ?
                    WHERE id = ?
                ");
                $stmt->execute([
                    $data['cikkszam'],
                    $data['nev'],
                    $data['leiras'] ?? '',
                    $data['kategoria_id'] ?? null,
                    $data['ar'],
                    $data['akcios_ar'] ?? null,
                    $data['keszlet'] ?? 0,
                    $data['gyarto'] ?? '',
                    $data['oe_szam'] ?? '',
                    $data['kep_url'] ?? '',
                    $id
                ]);
                
                echo json_encode(['success' => true, 'message' => 'Termek sikeresen frissitve']);
            }
            break;
            
        case 'delete':
            if ($method === 'POST') {
                $data = json_decode(file_get_contents('php://input'), true);
                $id = $data['id'] ?? 0;
                
                $pdo->prepare("DELETE FROM alkatresz_auto WHERE alkatresz_id = ?")->execute([$id]);
                $stmt = $pdo->prepare("DELETE FROM alkatreszek WHERE id = ?");
                $stmt->execute([$id]);
                
                echo json_encode(['success' => true, 'message' => 'Termek sikeresen torolve']);
            }
            break;
        
        case 'all':
            $stmt = $pdo->query("SELECT a.*, k.nev as kategoria_nev FROM alkatreszek a LEFT JOIN kategoriak k ON a.kategoria_id = k.id ORDER BY a.letrehozva DESC");
            echo json_encode(['success' => true, 'products' => $stmt->fetchAll(PDO::FETCH_ASSOC)]);
            break;
            
        case 'categories':
            $stmt = $pdo->query("SELECT * FROM kategoriak ORDER BY nev");
            echo json_encode(['success' => true, 'categories' => $stmt->fetchAll(PDO::FETCH_ASSOC)]);
            break;
            
        default:
            echo json_encode(['success' => false, 'error' => 'Ismeretlen muvelet']);
    }
} catch (PDOException $e) {
    echo json_encode(['success' => false, 'error' => 'Adatbazis hiba: ' . $e->getMessage()]);
} catch (Exception $e) {
    echo json_encode(['success' => false, 'error' => 'Szerver hiba: ' . $e->getMessage()]);
}
?>
