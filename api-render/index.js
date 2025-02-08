import renderApi from '@api/render-api';

renderApi.auth('rnd_zh7zMjbpCpuv2D7rhF7o6a6BbHFP');
renderApi.listServices({includePreviews: 'true', limit: '20'})
  .then(({ data }) => console.log(data))
  .catch(err => console.error(err));