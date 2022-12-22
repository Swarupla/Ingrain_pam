
#Created by Joydeep Sarkar
#Created on 09-Feb-2018
#Copyright MyWizard VA

import logging

#try:
#    from AI_NLP.shared.app_config import loglevel
#except Exception as e:
from shared.app_config import loglevel

class GenerateLogger:
    def __init__(self):
        self._logger = logging.getLogger()

    def generate_logger(self,filename):
        if   loglevel == 'INFO':
            self._logger.setLevel(logging.INFO)
        elif loglevel == 'WARNING':
            self._logger.setLevel(logging.WARNING)
        elif loglevel == 'ERROR':
            self._logger.setLevel(logging.ERROR)
        elif loglevel == 'EXCEPTION':
            self._logger.setLevel(logging.EXCEPTION)
        elif loglevel == 'CRITICAL':
            self._logger.setLevel(logging.CRITICAL)
        else:
            self._logger.setLevel(logging.DEBUG)

        if self._logger.handlers:
            self._logger.handlers = []  # Resets the logger.handlers if it already exists.

        fhandler = logging.FileHandler(filename=filename, mode='a', encoding = "UTF-8")
        formatter = logging.Formatter('%(asctime)s - %(name)s - %(levelname)s - %(message)s')
        fhandler.setFormatter(formatter)
        self._logger.addHandler(fhandler)
        return self._logger
