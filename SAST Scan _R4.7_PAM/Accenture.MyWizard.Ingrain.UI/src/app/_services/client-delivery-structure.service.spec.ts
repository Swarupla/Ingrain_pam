import { TestBed } from '@angular/core/testing';

import { ClientDeliveryStructureService } from './client-delivery-structure.service';

describe('ClientDeliveryStructureService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: ClientDeliveryStructureService = TestBed.get(ClientDeliveryStructureService);
    expect(service).toBeTruthy();
  });
});
