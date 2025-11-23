const express = require('express');
const jwt = require('jsonwebtoken');
const app = express();

const SECRET_KEY = "secret_key";

app.use(express.json());

// POST - Used to Create/Send datas to server
app.post('/login' , (req , res)=>{
    
    const { username, password } = req.body;

    // Simple Auth Logic (with DB)
    if(username === 'test' && password === '1234'){
        // Create JWT
        const token = jwt.sign(
            {username: username},
            SECRET_KEY,
            {expiresIn: '1h'}
        );

        // Return result
        res.json({success: true, token: token});
        console.log(`Login success : ${username}`);
    }
    else{
        // Return result
        res.json({success: false, message: 'Invalid credentials'});
        console.log(`Login FAILED : ${username}`);
    }
});

function authenticateToken(req, res, next) {
    const authHeader = req.headers['authorization'];
    const token = authHeader && authHeader.split(' ')[1];

    if (!token)
    {
        console.log('Token is invalid!');
        return res.sendStatus(401);
    }

    jwt.verify(token, SECRET_KEY, (err, user) => {
       if (err) {
        console.log('Token err occured!');
        return res.sendStatus(403);
       }
       req.user = user;
       next(); 
    });
}

// GET - Used to Look Up data in server
app.get('/profile', authenticateToken, (req , res)=>{
   res.json({message: 'Hello!', user: req.user});
})

const PORT = 61393;
app.listen(PORT, () => {
    console.log(`Authentication server running on port ${PORT}`);
});