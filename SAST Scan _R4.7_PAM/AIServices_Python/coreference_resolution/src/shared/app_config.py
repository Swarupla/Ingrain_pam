#Created by Joydeep Sarkar
#Created on 09-Feb-2018
#Copyright MyWizard VA

loglevel = 'DEBUG'
#loglevel = 'INFO'
#loglevel = 'WARNING'
#loglevel = 'ERROR'
#loglevel = 'EXCEPTION'
#loglevel = 'CRITICAL'

environment = 'local'
# environment = 'dev'
# environment = 'stage'
# environment = 'prod'

if(environment == 'local'):
    default_model_name = 'NLP_Default_Model'
elif(environment == 'dev'):
   default_model_name = 'model_20181220-093311'
elif(environment == 'stage'):
    default_model_name = 'model_20181220-093311'
elif(environment == 'prod'):
    default_model_name = 'model_20181220-093311'