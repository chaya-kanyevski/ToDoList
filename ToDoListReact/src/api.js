import axios from 'axios';

axios.defaults.baseURL =process.env.REACT_APP_API_URL;

axios.defaults.headers.post['Content-Type'] = 'application/json';


axios.interceptors.response.use(
    response => response,
    error => {
      console.error('API Error:', error.response ? error.response.data : error.message);
      return Promise.reject(error); 
    }
  );

export default axios;