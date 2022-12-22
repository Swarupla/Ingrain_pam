# -*- coding: utf-8 -*-
"""
Created on Tue Jun 16 11:45:57 2020

@author: m.hari.abhishek
"""
import datetime
from datetime import datetime
from flask import Flask, request, Response
from flask import jsonify
from whatif import whatIf
from SimulationProcess import Simulation
import Auth
import pymongo
import utils
#import datetime
import subprocess 
import configparser, os
import json
#config = configparser.RawConfigParser()
#conf_path = "/pythonconfig.ini"
#configpath = str(os.getcwd()) + str(conf_path)
#config.read(configpath)



import file_encryptor
config = configparser.RawConfigParser()
configpath = "./pythonconfig.ini"
try:
    config.read(configpath)
except UnicodeDecodeError:   
    config = file_encryptor.get_configparser_obj(configpath)


app = Flask(__name__)


import platform
if platform.system() == 'Linux':
    prefix="/monteCarlo"
elif platform.system() == 'Windows':
    prefix="/monteCarlo"
@app.route(prefix)
def index():
    return "MonteCarlo Python API"

@app.route(prefix+"/runSimulation",methods=["POST","GET"])
def monteCarlo():
    if request.method == "GET":
        return "Get method not allowed"
    #if platform.system() == 'Linux':
    #    try:
    #        token = request.headers['Authorization']
    #        token = token.strip("Bearer").strip("bearer").strip()
    #        if Auth.validateToken(token) == 'Fail':
    #            return Response(status=401)
    #    except:
    #        return Response(status=401)
    #elif platform.system() == 'Windows':
    #    pass
    
    r = eval(str(request.json))
    TemplateID = r["TemplateID"]
    logger = utils.logger('Get', TemplateID)
#    try:
    message = {"Status":Simulation(TemplateID)}
    utils.logger(logger, TemplateID, 'INFO', (str(r) + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))))
    utils.logger(logger, TemplateID, 'INFO', ('invokeMonteCarlo runsimulation' + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))))
    return jsonify(message)
    #except ValueError:
        #message = {"Error" :"ValueError"}
    #except pymongo.errors.CursorNotFound:
        #message = {"Error" :"Invalid parameters for Mongo"}
    #except pymongo.errors.DuplicateKeyError:
        #message = {"Error" :"Duplicate Key Error"}
    #except IndexError:
        #message = {"Error" :"Index Error"}
    #except TypeError:
        #message = {"Error" :"Type Error"}
#    except Exception as e :
#        message = {"Error" :str(e.args[0])} 
#    return Response(str(message),status=500)

@app.route(prefix+"/whatIfAnalysis",methods=["POST","GET"])
def whatIfAnalysis():
    if request.method == "GET":
        return "Get method not allowed"
    #if platform.system() == 'Linux':
    #    try:
    #        token = request.headers['Authorization']
    #        token = token.strip("Bearer").strip("bearer").strip()
    #        if Auth.validateToken(token) == 'Fail':
    #            return Response(status=401)
    #    except:
    #        return Response(status=401)
    #elif platform.system() == 'Windows':
    #    pass
    
    r = eval(str(request.json))
    TemplateID = r["TemplateID"]
    SimulationID = r["SimulationID"]
    try:
        inputs = eval(r["inputs"])
    except Exception as e:
        inputs = json.loads(r["inputs"])
    logger = utils.logger('Get', TemplateID)
    message = whatIf(TemplateID,SimulationID,inputs)
    utils.logger(logger, TemplateID, 'INFO', (str(r) + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))))
    utils.logger(logger, TemplateID, 'INFO', ('invokeMonteCarlo whatIfAnalysis' + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))))
    return jsonify(message)
#    except ValueError:
#        message = {"Error" :"ValueError"}
#    except pymongo.errors.CursorNotFound:
#        message = {"Error" :"Invalid parameters for Mongo"}
#    except pymongo.errors.DuplicateKeyError:
#        message = {"Error" :"Duplicate Key Error"}
#    except IndexError:
#        message = {"Error" :"Index Error"}
#    except TypeError:
#        message = {"Error" :"Type Error"}
#    except Exception as e:
#        message = {"Error" :str(e.args[0])}     
#    return Response(str(message),status=500)

@app.route(prefix+"/IngestRRPData",methods=["POST","GET"])
def IngestRRPData():
    try: 
        if request.method == "GET":
            return "Get method not allowed"
        r                =   eval(str(request.json))
        cid              =   r["ClientUID"]
        dcuid            =   r["DeliveryConstructUID"]
        TemplateID       =   r["TemplateID"]
        version          =   r["Version"]
        path             =   config["PYTHONPATH"]['path']
        args             =   cid+" "+dcuid+" "+TemplateID+" "+version
        pyFileName       =   str(os.getcwd())+"/IngestPhoenixData.py"
        cmd              =   path+" "+pyFileName+" "+args
        logger = utils.logger('Get', TemplateID)
        subprocess.Popen(cmd,close_fds=True,shell=True)
        utils.logger(logger, TemplateID, 'INFO', (str(cmd)  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))))
        utils.logger(logger, TemplateID, 'INFO', ('invokeMonteCarlo Data ingestion completed successfully'  + str(datetime.today().strftime('%Y-%m-%d %H:%M:%S'))))
    except Exception as e:
        logger = utils.logger('Get', TemplateID)
        utils.logger(logger, TemplateID, 'ERROR', 'Trace')
        return jsonify({'status': 'false', 'message':str(e.args)}), 500
    return jsonify({'status': 'True', 'message':"Success"}), 200
  
if __name__ == "__main__":
    app.run()
