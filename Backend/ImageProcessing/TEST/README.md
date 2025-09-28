# What to do?

1. Set Up instructions
2. Run instructions
3. All you need to do is run everything. A ```MapSegmentationGenerator.keras``` file will be created. This is what I need. It is the CNN model that I need.
4. Push the results (not the data files) to the repo.

## Set Up Instructions

1. Download the data from the shared Quintessential google drive. It is called data.zip.

2.  Extract the data into the TEST folder. Ensure it has this folder structure:
Backend/ImageProcessing/TEST/data/

3. Navigate to the TEST folder:
```bash
cd Backend/ImageProcessing/TEST
```

## Run Instructions

1. Create a virtual environment:
```bash
python3 -m venv venv
```
2. Run the following command to activate the virtual environment:
```bash
source venv/bin/activate
```
3. The following command will take a while to install everything.
```bash
pip install -r requirements.txt
```
4. There are 2 ways to run the system so that it will run the tests correctly.
- Open the mapunet.ipynb file and select "Run All" from the "Cell" menu.
- Run the following command (ensue you are in the venv when you do this):
```bash
 jupyter notebook
```
This will say it wants to open in 8888 in your browser. it might ask for a token password. that would appear in the terminal like so:
```bash
[I 2025-08-10 20:37:39.678 ServerApp] Jupyter Server 2.16.0 is running at:
[I 2025-08-10 20:37:39.678 ServerApp] http://localhost:8888/tree?token=bc23ef0eb72dc7b52e422a518465b24433766efcd529eae5
```
Your's will be different, so copy that token and paste it into the browser when it asks for a password.
Once that is done you can click on the mapunet and then click on the play button to run the tests. But you will need to spam click on it until you get to the bottom of the project.

## Errors you may face
If you get warnings about cuda, it's ok. just ignore it and continue. It will not stop the system. It's just warning you about how it is being run.




