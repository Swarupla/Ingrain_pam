import torch
d='/var/www/myWizard.IngrAInAIServices.WebAPI.Python'
torch.hub.set_dir(d)
print("Torch updated path---",torch.hub.get_dir())
en2fr = torch.hub.load('pytorch/fairseq', 'dynamicconv.glu.wmt14.en-fr', tokenizer='moses', bpe='subword_nmt')
ru2en = torch.hub.load('pytorch/fairseq', 'transformer.wmt19.ru-en.single_model', tokenizer='moses', bpe='fastbpe')
en2de = torch.hub.load('pytorch/fairseq', 'transformer.wmt19.en-de.single_model', tokenizer='moses', bpe='fastbpe')
de2en = torch.hub.load('pytorch/fairseq', 'transformer.wmt19.de-en.single_model', tokenizer='moses', bpe='fastbpe')
en2ru = torch.hub.load('pytorch/fairseq', 'transformer.wmt19.en-ru.single_model', tokenizer='moses', bpe='fastbpe')
