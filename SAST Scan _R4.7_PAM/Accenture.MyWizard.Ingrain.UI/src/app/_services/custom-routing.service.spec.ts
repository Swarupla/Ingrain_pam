import { TestBed } from '@angular/core/testing';

import { CustomRoutingService } from './custom-routing.service';

describe('CustomRoutingService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: CustomRoutingService = TestBed.get(CustomRoutingService);
    expect(service).toBeTruthy();
  });
});
