import { TestBed } from '@angular/core/testing';

import { AdModelEngineeringService } from './ad-model-engineering.service';

describe('AdModelEngineeringService', () => {
  let service: AdModelEngineeringService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(AdModelEngineeringService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
