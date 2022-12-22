#import sys
#sys.path.insert(0, "/var/www/myWizard.IngrAInAIServices.WebAPI.Python/intent_entity/src")

from Ingrain_AIService import app 
print('main hello')
application = app

if __name__ == "__main__":
    application.run()
