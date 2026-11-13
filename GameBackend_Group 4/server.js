const express = require('express');
const cors = require('cors');
const db = require('./database');

const app = express();
const PORT = process.env.PORT || 5000;

// Middleware
app.use(cors()); // Menerima request dari client Unity
app.use(express.json()); // Menerima payload format JSON

// Endpoint Health Check (Pengujian Awal)
app.get('/api/health', (req, res) => {
    res.status(200).json({
        status: 'success',
        message: 'Game Backend Server is running successfully!',
        serverTime: new Date().toISOString()
    });
});

// Draft Endpoint API Spesifikasi
app.post('/api/register', (req, res) => {
    res.status(501).json({ message: 'Endpoint /api/register belum diimplementasikan' });
});

app.post('/api/login', (req, res) => {
    res.status(501).json({ message: 'Endpoint /api/login belum diimplementasikan' });
});

// --- ENDPOINT GET LEADERBOARD (TOP 10 HIGH SCORES) ---
app.get('/api/leaderboard', (req, res) => {
    const sql = `
    SELECT u.username, MAX(s.score) AS highScore
    FROM scores s
    JOIN users u ON s.user_id = u.id
    GROUP BY s.user_id
    ORDER BY highScore DESC
    LIMIT 10
    `;

 db.all(sql, [], (err, rows) => {
    if (err) {
        console.error('Gagal mengambil leaderboard:', err.message);
        return res.status(500).json({ status: 'error', message: 'Gagal mengambil data leaderboard.' });
        }
        
        res.status(200).json({
            status: 'success',
            data: rows
        });
    });
});

// --- ENDPOINT 1: SIMPAN SKOR PERTANDINGAN (MODUL 11) ---
app.post('/api/score/save', (req, res) => {
    const { userId, score } = req.body;

    if (userId === undefined || score === undefined) {
        return res.status(400).json({
            status: 'error',
            message: 'userId dan score wajib diisi!'
        });
    }

    const sql = `INSERT INTO scores (user_id, score) VALUES (?, ?)`;
    db.run(sql, [userId, score], function (err) {
        if (err) {
            console.error('Gagal menyimpan skor:', err.message);
            return res.status(500).json({
                status: 'error',
                message: 'Gagal menyimpan skor ke database.'
            });
        }

        console.log(`[SCORE SAVED] UserID: ${userId}, Score: ${score}`);
        res.status(201).json({
            status: 'success',
            message: 'Skor berhasil disimpan!',
            scoreId: this.lastID
        });
    });
});

// --- ENDPOINT 2: AMBIL SKOR TERTINGGI PEMAIN (PERSONAL HIGH SCORE) (MODUL 11) ---
app.get('/api/score/highscore/:userId', (req, res) => {
    const userId = req.params.userId;
    const sql = `SELECT MAX(score) AS highScore FROM scores WHERE user_id = ?`;

    db.get(sql, [userId], (err, row) => {
        if (err) {
            console.error('Gagal mengambil high score:', err.message);
            return res.status(500).json({
                status: 'error',
                message: 'Gagal mengambil data dari database.'
            });
        }

        // Jika pemain belum pernah memiliki skor, kembalikan 0
        const personalHighScore = (row && row.highScore !== null) ? row.highScore : 0;
        console.log(`[HIGH SCORE FETCH] UserID: ${userId}, HighScore: ${personalHighScore}`);

        res.status(200).json({
            status: 'success',
            userId: parseInt(userId, 10),
            highScore: personalHighScore
        });
    });
});

// Jalankan Server
app.listen(PORT, () => {
    console.log(`=================================`);
    console.log(` Server Game Backend Berjalan!`);
    console.log(` URL: http://localhost:${PORT}`);
    console.log(`=================================`);
});
