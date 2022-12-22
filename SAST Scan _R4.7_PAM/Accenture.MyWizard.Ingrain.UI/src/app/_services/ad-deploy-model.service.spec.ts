import { TestBed } from '@angular/core/testing';

import { AdDeployModelService } from './ad-deploy-model.service';

describe('AdDeployModelService', () => {
  let service: AdDeployModelService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(AdDeployModelService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
