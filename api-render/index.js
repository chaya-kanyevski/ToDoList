const express = require('express');
const axios = require('axios');
require('dotenv').config();

const app = express();
const PORT = process.env.PORT || 3001;


app.get('/', async (req, res) => {
  try {
    const apiKey = process.env.REACT_APP_API_URL;

    const response = await axios.get('https://api.render.com/v1/services', {
      headers: {
        Authorization: `Bearer ${apiKey}`,
      },
    });

    res.json(response.data);
  } catch (error) {
    console.error('Error fetching data from Render API:', error);
    res.status(500).json({ message: 'Error fetching data' });
  }
});


app.listen(PORT, () => {
  console.log(`Server is running on port ${PORT}`);
});