import { TestBed } from '@angular/core/testing';

import { AdRecommendedService } from './ad-recommended.service';

describe('AdRecommendedService', () => {
  let service: AdRecommendedService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(AdRecommendedService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
