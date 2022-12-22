import { TestBed } from '@angular/core/testing';

import { DeployModelService } from './deploy-model.service';

describe('DeployModelService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: DeployModelService = TestBed.get(DeployModelService);
    expect(service).toBeTruthy();
  });
});
