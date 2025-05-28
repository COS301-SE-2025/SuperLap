const { app, connectToDb } = require('./app');

const port = process.env.PORT || 3000;

connectToDb().then(() => {
  app.listen(port, () => {
    console.log(`Server running at http://localhost:${port}`);
  });
});