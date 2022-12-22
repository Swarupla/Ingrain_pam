import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdDeployedModelComponent } from './ad-deployed-model.component';

describe('AdDeployedModelComponent', () => {
  let component: AdDeployedModelComponent;
  let fixture: ComponentFixture<AdDeployedModelComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ AdDeployedModelComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(AdDeployedModelComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
