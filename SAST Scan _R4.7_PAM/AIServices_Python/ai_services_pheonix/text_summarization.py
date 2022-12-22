# -*- coding: utf-8 -*-
"""
Created on Mon Feb  1 13:08:31 2021

@author: abhilasha.lodha
"""
import time
start=time.time()
import os,sys,inspect
currentdir = os.path.dirname(os.path.abspath(inspect.getfile(inspect.currentframe())))
parentdir = os.path.dirname(currentdir)
sys.path.insert(0,parentdir)
import utils
from pathlib import Path
mainDirectory = str(Path(__file__).parent.parent.absolute())
end=time.time()

def getSummarization(minW,maxW,text,correlationId,uniId,summary_path):
    start_time = time.time()
    import torch
    device = torch.device('cpu')
    from transformers import T5Tokenizer, T5ForConditionalGeneration
    model = T5ForConditionalGeneration.from_pretrained(summary_path)
    tokenizer = T5Tokenizer.from_pretrained(summary_path)
    logger = utils.logger('Get', correlationId)
    cpu,memory=utils.Memorycpu()
    utils.logger(logger, correlationId, 'INFO', ('In getSummarization function took '+ str(end-start) + ' secs'+ " with CPU: "+cpu+" Memory: "+memory),str(uniId))
    preprocess_text = text.strip().replace("\n","")
    t5_prepared_Text = "summarize: "+preprocess_text
    tokenized_text = tokenizer.encode(t5_prepared_Text, return_tensors="pt").to(device)
    # summmarize
    utils.logger(logger, correlationId, 'INFO', ('Rcvd parameters for summary---> minW'+str(minW)+'---maxW'+str(maxW)+'--text--'+str(text)),str(uniId))
    summary_ids = model.generate(tokenized_text,
                                        num_beams=4,
                                        no_repeat_ngram_size=2,
                                        min_length=minW,
                                        max_length=maxW,
                                        early_stopping=False)
    
    output = tokenizer.decode(summary_ids[0], skip_special_tokens=True)
    utils.logger(logger, correlationId, 'INFO', ('output for text summary is complete'),str(uniId))
    end_time = time.time()
    utils.logger(logger, correlationId, 'INFO', ('Total time took for getsummary function in seconds is {}'.format(end_time-start_time)),str(uniId))
    return output 
    
    




