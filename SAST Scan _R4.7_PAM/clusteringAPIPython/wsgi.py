import sys
import os
import platform
if platform.system() == 'Linux':
    sys.path.insert(0, os.getcwd())
elif platform.system() == 'Windows':
    pass

from webapp import app 
application = app

if __name__ == "__main__":
    application.run()
