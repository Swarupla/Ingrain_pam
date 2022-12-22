import sys
import os
sys.path.insert(0, os.getcwd())

from invokeInferenceEngine import app 
application = app

if __name__ == "__main__":
    application.run()
