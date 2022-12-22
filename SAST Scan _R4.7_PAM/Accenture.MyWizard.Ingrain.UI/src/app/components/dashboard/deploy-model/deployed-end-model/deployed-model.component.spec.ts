import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { DeployedModelComponent } from './deployed-model.component';

describe('DeployedModelComponent', () => {
  let component: DeployedModelComponent;
  let fixture: ComponentFixture<DeployedModelComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ DeployedModelComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(DeployedModelComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
